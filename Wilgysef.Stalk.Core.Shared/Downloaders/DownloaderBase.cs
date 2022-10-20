using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Wilgysef.Stalk.Core.Shared.Extensions;
using Wilgysef.Stalk.Core.Shared.FilenameSlugs;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;
using Wilgysef.Stalk.Core.Shared.MetadataSerializers;
using Wilgysef.Stalk.Core.Shared.StringFormatters;

namespace Wilgysef.Stalk.Core.Shared.Downloaders
{
    public abstract class DownloaderBase : IDownloader
    {
        public static class ConfigKeys
        {
            /// <summary>
            /// Config key for setting whether filename and metadata templates should be saved as metadata.
            /// </summary>
            public const string SaveFilenameTemplatesMetadata = "saveFilenameTemplatesMetadata";

            /// <summary>
            /// Whether a file should be skipped if one with the same name already exists.
            /// </summary>
            public const string SkipExisting = "skipExisting";
        }

        /// <summary>
        /// Buffer size for downloading files.
        /// </summary>
        public virtual int DownloadBufferSize
        {
            get => _downloadBufferSize;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Buffer size must be greater than zero.");
                }
                _downloadBufferSize = value;
            }
        }
        private int _downloadBufferSize = 4 * 1024;

        /// <summary>
        /// Hash name used in <see cref="HashAlgorithm.Create(string)"/>.
        /// </summary>
        public virtual string? HashName { get; set; } = "SHA256";

        public abstract string Name { get; }

        public virtual ILogger? Logger { get; set; }

        public virtual IDictionary<string, object?> Config { get; set; } = new Dictionary<string, object?>();

        private readonly IFileSystem _fileSystem;
        private readonly IStringFormatter _stringFormatter;
        private readonly IFilenameSlugSelector _filenameSlugSelector;
        private readonly IMetadataSerializer _metadataSerializer;
        private HttpClient _httpClient;

        protected DownloaderBase(
            IFileSystem fileSystem,
            IStringFormatter stringFormatter,
            IFilenameSlugSelector filenameSlugSelector,
            IMetadataSerializer metadataSerializer,
            HttpClient httpClient)
        {
            _fileSystem = fileSystem;
            _stringFormatter = stringFormatter;
            _filenameSlugSelector = filenameSlugSelector;
            _metadataSerializer = metadataSerializer;
            _httpClient = httpClient;
        }

        public abstract bool CanDownload(Uri uri);

        public virtual async IAsyncEnumerable<DownloadResult> DownloadAsync(
            Uri uri,
            string filenameTemplate,
            string? itemId,
            string? metadataFilenameTemplate,
            IMetadataObject metadata,
            DownloadRequestData? requestData = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (Config.TryGetValueAs<bool, string, object?>(ConfigKeys.SaveFilenameTemplatesMetadata, out var saveFilenameTemplatesMetadata)
                && saveFilenameTemplatesMetadata)
            {
                metadata.TryAddValue(filenameTemplate, MetadataObjectConsts.File.FilenameTemplateKeys);
                metadata.TryAddValue(metadataFilenameTemplate, MetadataObjectConsts.MetadataFilenameTemplateKeys);
            }

            metadata.TryAddValue(itemId, MetadataObjectConsts.Origin.ItemIdKeys);
            metadata.TryAddValue(itemId, MetadataObjectConsts.Origin.ItemIdSeqKeys);
            metadata.TryAddValue(uri.AbsoluteUri, MetadataObjectConsts.Origin.UriKeys);

            await foreach (var downloadFileResult in SaveFileAsync(
                uri,
                filenameTemplate,
                metadata,
                requestData,
                cancellationToken))
            {
                var resultMetadata = metadata.Copy();
                resultMetadata.TryAddValue(DateTimeOffset.Now.ToString(), MetadataObjectConsts.RetrievedKeys);

                if (downloadFileResult.FileSize.HasValue)
                {
                    resultMetadata.TryAddValue(downloadFileResult.FileSize, MetadataObjectConsts.File.SizeKeys);
                }
                if (downloadFileResult.Hash != null)
                {
                    resultMetadata.TryAddValue(downloadFileResult.Hash, MetadataObjectConsts.File.HashKeys);
                    resultMetadata.TryAddValue(downloadFileResult.HashName, MetadataObjectConsts.File.HashAlgorithmKeys);
                }

                string? metadataFilename;
                if (downloadFileResult.MetadataFilename == null && downloadFileResult.CreateMetadata)
                {
                    metadataFilename = await SaveMetadataAsync(
                        metadataFilenameTemplate,
                        resultMetadata,
                        cancellationToken);
                }
                else
                {
                    metadataFilename = downloadFileResult.MetadataFilename;
                }

                yield return new DownloadResult(
                    downloadFileResult.Filename,
                    uri,
                    itemId,
                    metadataPath: metadataFilename,
                    metadata: resultMetadata);
            }
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
        protected virtual async IAsyncEnumerable<DownloadFileResult> SaveFileAsync(
            Uri uri,
            string filenameTemplate,
            IMetadataObject metadata,
            DownloadRequestData? requestData = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var filename = GetFormattedSlugifiedFilename(filenameTemplate, metadata, false);

            if (Config.TryGetValueAs<bool, string, object?>(ConfigKeys.SkipExisting, out var skipExisting)
                && skipExisting
                && _fileSystem.File.Exists(filename))
            {
                Logger?.LogInformation("Skipping download from {Uri}, file already exists: {Filename}", uri.AbsoluteUri, filename);
                yield break;
            }

            CreateDirectoriesFromFilename(filename);

            using var stream = await GetFileStreamAsync(uri, requestData, cancellationToken);
            using var fileStream = _fileSystem.File.Open(filename, FileMode.CreateNew);

            yield return await SaveStreamAsync(
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
            DownloadRequestData? requestData = null,
            CancellationToken cancellationToken = default)
        {
            var response = requestData != null
                ? await _httpClient.SendAsync(requestData.CreateRequest(uri), cancellationToken)
                : await _httpClient.GetAsync(uri, cancellationToken);
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
        /// <param name="buffer">
        /// Buffer to use for writing to output stream and computing hash.
        /// If <see langword="null"/>, creates a new <see langword="byte"/>[] with size of <see cref="DownloadBufferSize"/>.
        /// </param>
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

                hashAlgorithm?.TransformBlock(buffer, 0, bytesRead, null, 0);

                if (bytesRead == 0)
                {
                    break;
                }
            }

            hashAlgorithm?.TransformFinalBlock(buffer, 0, 0);

            return new DownloadFileResult(
                filename,
                null,
                fileSize,
                hashAlgorithm?.Hash != null ? hashAlgorithm.Hash.ToHexString() : null,
                hashAlgorithm?.Hash != null ? hashName : null,
                createMetadata: true);
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
            IMetadataObject metadata,
            CancellationToken cancellationToken = default)
        {
            if (metadataFilenameTemplate == null)
            {
                return Task.FromResult<string?>(null);
            }

            var metadataFilename = GetFormattedSlugifiedFilename(metadataFilenameTemplate, metadata, true);

            CreateDirectoriesFromFilename(metadataFilename);

            try
            {
                using var stream = _fileSystem.File.Open(metadataFilename, FileMode.CreateNew);
                using var writer = new StreamWriter(stream);
                _metadataSerializer.Serialize(writer, metadata.GetDictionary());
            }
            catch (IOException exception)
            {
                Logger?.LogError(exception, "Could not write metadata to {Path}", metadataFilename);
            }

            return Task.FromResult<string?>(metadataFilename);
        }

        /// <summary>
        /// Gets the formatted and slugified filename.
        /// </summary>
        /// <param name="filenameTemplate">Filename template.</param>
        /// <param name="formatValues">Format values.</param>
        /// <param name="metadata">Metadata.</param>
        /// <param name="isMetadataFilename">Whether the filename is intended for metadata files.</param>
        /// <returns>Formatted and slugified filename.</returns>
        protected string GetFormattedSlugifiedFilename(
            string filenameTemplate,
            IDictionary<string, object?> formatValues,
            IMetadataObject metadata,
            bool isMetadataFilename)
        {
            var formattedFilename = _stringFormatter.Format(filenameTemplate, formatValues);

            if (formattedFilename == filenameTemplate)
            {
                formattedFilename = _fileSystem.Path.Combine(
                    filenameTemplate,
                    GetDefaultFilename(metadata, isMetadataFilename));
            }

            return _filenameSlugSelector
                .GetFilenameSlugByPlatform()
                .SlugifyPath(formattedFilename);
        }

        /// <summary>
        /// Gets the formatted and slugified filename.
        /// </summary>
        /// <param name="filenameTemplate">Filename template.</param>
        /// <param name="metadata">Metadata.</param>
        /// <param name="isMetadataFilename">Whether the filename is intended for metadata files.</param>
        /// <returns>Formatted and slugified filename.</returns>
        protected string GetFormattedSlugifiedFilename(
            string filenameTemplate,
            IMetadataObject metadata,
            bool isMetadataFilename = false)
        {
            return GetFormattedSlugifiedFilename(
                filenameTemplate,
                metadata.GetFlattenedDictionary(MetadataObjectConsts.Separator),
                metadata,
                isMetadataFilename);
        }

        /// <summary>
        /// Gets the default filename from metadata.
        /// </summary>
        /// <param name="metadata">Metadata.</param>
        /// <param name="isMetadataFilename">Indicates if the filename will be used for a metadata filename.</param>
        /// <returns>Default filename, or <see langword="null"/> if unknown.</returns>
        protected string? GetDefaultFilename(IMetadataObject metadata, bool isMetadataFilename)
        {
            if (!metadata.TryGetValue(out var itemIdSeq, MetadataObjectConsts.Origin.ItemIdSeqKeys)
                || itemIdSeq == null)
            {
                return null;
            }

            var defaultFilename = itemIdSeq.ToString();
            if (!isMetadataFilename)
            {
                if (metadata.TryGetValue(out var extension, MetadataObjectConsts.File.ExtensionKeys)
                    && extension != null)
                {
                    defaultFilename += "." + extension;
                }
            }
            else
            {
                defaultFilename += ".meta.txt";
            }
            return defaultFilename;
        }

        /// <summary>
        /// Creates directories from filename path if directories do not exist.
        /// </summary>
        /// <param name="filename">Filename path.</param>
        protected void CreateDirectoriesFromFilename(string filename)
        {
            var dirname = _fileSystem.Path.GetDirectoryName(filename);
            if (!string.IsNullOrEmpty(dirname))
            {
                _fileSystem.Directory.CreateDirectory(dirname);
            }
        }
    }
}
