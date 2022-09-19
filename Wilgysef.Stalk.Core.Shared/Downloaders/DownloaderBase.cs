using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wilgysef.Stalk.Core.Shared.FilenameSlugs;
using Wilgysef.Stalk.Core.Shared.FileServices;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;
using Wilgysef.Stalk.Core.Shared.MetadataSerializers;
using Wilgysef.Stalk.Core.Shared.StringFormatters;

namespace Wilgysef.Stalk.Core.Shared.Downloaders
{
    public abstract class DownloaderBase : IDownloader
    {
        /// <summary>
        /// Buffer size for downloading files.
        /// </summary>
        public virtual int DownloadBufferSize { get; set; } = 4 * 1024;

        /// <summary>
        /// Hash name used in <see cref="HashAlgorithm.Create(string)"/>.
        /// </summary>
        public virtual string? HashName { get; set; } = "SHA256";

        public virtual string Name => "Default";

        public virtual ILogger? Logger { get; set; }

        public virtual IDictionary<string, object?> Config { get; set; } = new Dictionary<string, object?>();

        private readonly IFileService _fileService;
        private readonly IStringFormatter _stringFormatter;
        private readonly IFilenameSlugSelector _filenameSlugSelector;
        private readonly IMetadataSerializer _metadataSerializer;
        private HttpClient _httpClient;

        public DownloaderBase(
            IFileService fileService,
            IStringFormatter stringFormatter,
            IFilenameSlugSelector filenameSlugSelector,
            IMetadataSerializer metadataSerializer,
            HttpClient httpClient)
        {
            _fileService = fileService;
            _stringFormatter = stringFormatter;
            _filenameSlugSelector = filenameSlugSelector;
            _metadataSerializer = metadataSerializer;
            _httpClient = httpClient;
        }

        public virtual bool CanDownload(Uri uri)
        {
            return true;
        }

        public virtual async IAsyncEnumerable<DownloadResult> DownloadAsync(
            Uri uri,
            string filenameTemplate,
            string? itemId,
            string? itemData,
            string? metadataFilenameTemplate,
            IMetadataObject metadata,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var downloadFileResult = await SaveFileAsync(
                uri,
                filenameTemplate,
                metadata,
                cancellationToken);

            metadata.TryAddValueByParts(filenameTemplate, MetadataObjectConsts.File.FilenameTemplateKeys);
            metadata.TryAddValueByParts(metadataFilenameTemplate, MetadataObjectConsts.MetadataFilenameTemplateKeys);
            metadata.TryAddValueByParts(itemId, MetadataObjectConsts.Origin.ItemIdKeys);
            metadata.TryAddValueByParts(uri.ToString(), MetadataObjectConsts.Origin.UriKeys);
            metadata.TryAddValueByParts(DateTime.Now, MetadataObjectConsts.RetrievedKeys);

            metadata.TryAddValueByParts(downloadFileResult.FileSize, MetadataObjectConsts.File.SizeKeys);
            if (downloadFileResult.Hash != null)
            {
                metadata.TryAddValueByParts(downloadFileResult.Hash, MetadataObjectConsts.File.HashKeys);
                metadata.TryAddValueByParts(downloadFileResult.HashName, MetadataObjectConsts.File.HashAlgorithmKeys);
            }

            var metadataFilename = await SaveMetadataAsync(
                metadataFilenameTemplate,
                metadata.GetFlattenedDictionary(),
                cancellationToken);

            yield return new DownloadResult(
                downloadFileResult.Filename,
                uri,
                itemId,
                itemData: itemData,
                metadataPath: metadataFilename,
                metadata: metadata);
        }

        public virtual void SetHttpClient(HttpClient client)
        {
            _httpClient = client;
        }

        /// <summary>
        /// Downloads from the URI and saves it to a file.
        /// </summary>
        /// <param name="uri">URI to download from.</param>
        /// <param name="filenameTemplate">Filename template to save downloaded file.</param>
        /// <param name="metadata">Metadata object.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Download file result.</returns>
        protected virtual async Task<DownloadFileResult> SaveFileAsync(
            Uri uri,
            string filenameTemplate,
            IMetadataObject metadata,
            CancellationToken cancellationToken = default)
        {
            var filenameSlug = _filenameSlugSelector.GetFilenameSlugByPlatform();
            var filename = filenameSlug.SlugifyPath(
                _stringFormatter.Format(filenameTemplate, metadata.GetFlattenedDictionary()));

            using var stream = await GetFileStreamAsync(uri, cancellationToken);
            using var fileStream = _fileService.Open(filename, FileMode.CreateNew);

            return await SaveStreamAsync(
                stream,
                fileStream,
                filename,
                null,
                HashName != null ? HashAlgorithm.Create(HashName) : null,
                HashName,
                cancellationToken);
        }

        /// <summary>
        /// Gets the contents stream from the URI.
        /// </summary>
        /// <param name="uri">URI.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Stream with download contents.</returns>
        protected virtual async Task<Stream> GetFileStreamAsync(
            Uri uri,
            CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync(uri, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStreamAsync();
        }

        /// <summary>
        /// Reads the download stream and writes to the output stream.
        /// <para>
        /// The download stream is read in chunks into <paramref name="buffer"></paramref> and written to the output stream.
        /// If <paramref name="hashAlgorithm"/> is specified, also computes the hash while downloading.
        /// </para>
        /// </summary>
        /// <param name="stream">Download stream.</param>
        /// <param name="output">Output stream.</param>
        /// <param name="filename">Filename, only passed to return value.</param>
        /// <param name="buffer">Buffer to use for writing to output stream and computing hash. If <see langword="null"/>, creates a new <see langword="byte"/>[] with size of <see cref="DownloadBufferSize"/>.</param>
        /// <param name="hashAlgorithm">Hash algorithm for computing hash.</param>
        /// <param name="hashName">Hash name, only passed to return value.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Download file results.</returns>
        protected virtual async Task<DownloadFileResult> SaveStreamAsync(
            Stream stream,
            Stream output,
            string filename,
            byte[]? buffer = null,
            HashAlgorithm? hashAlgorithm = null,
            string? hashName = null,
            CancellationToken cancellationToken = default)
        {
            long fileSize = 0;
            buffer ??= new byte[DownloadBufferSize];

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                fileSize += bytesRead;

                cancellationToken.ThrowIfCancellationRequested();
                await output.WriteAsync(buffer, 0, bytesRead, cancellationToken);

                if (hashAlgorithm != null)
                {
                    hashAlgorithm.TransformBlock(buffer, 0, bytesRead, null, 0);
                }

                if (bytesRead == 0)
                {
                    break;
                }
            }

            if (hashAlgorithm != null)
            {
                hashAlgorithm.TransformFinalBlock(buffer, 0, 0);
            }

            return new DownloadFileResult(
                filename,
                fileSize,
                hashAlgorithm?.Hash != null ? ToHexString(hashAlgorithm.Hash) : null,
                hashAlgorithm?.Hash != null ? hashName : null);
        }

        /// <summary>
        /// Saves metadata to file.
        /// </summary>
        /// <param name="metadataFilenameTemplate">Metadata filename template to save metadata files.</param>
        /// <param name="metadata">Metadata object.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Metadata filename.</returns>
        protected virtual Task<string?> SaveMetadataAsync(
            string? metadataFilenameTemplate,
            IDictionary<string, object?> metadata,
            CancellationToken cancellationToken = default)
        {
            if (metadataFilenameTemplate == null)
            {
                return Task.FromResult<string?>(null);
            }

            var filenameSlug = _filenameSlugSelector.GetFilenameSlugByPlatform();
            var metadataFilename = filenameSlug.SlugifyPath(
                _stringFormatter.Format(metadataFilenameTemplate, metadata));

            try
            {
                using var stream = _fileService.Open(metadataFilename, FileMode.CreateNew);
                using var writer = new StreamWriter(stream);
                _metadataSerializer.Serialize(writer, metadata);
            }
            catch (IOException exception)
            {
                Logger?.LogError(exception, "Could not write metadata to {Path}", metadataFilename);
            }

            return Task.FromResult<string?>(metadataFilename);
        }

        private const string HexAlphabet = "0123456789abcdef";

        private static string ToHexString(byte[] bytes)
        {
            StringBuilder result = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
            {
                result.Append(HexAlphabet[b >> 4]);
                result.Append(HexAlphabet[b & 15]);
            }
            return result.ToString();
        }
    }
}
