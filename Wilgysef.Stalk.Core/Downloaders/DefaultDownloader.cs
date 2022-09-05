using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using Wilgysef.Stalk.Core.FilenameSlugs;
using Wilgysef.Stalk.Core.FileServices;
using Wilgysef.Stalk.Core.Shared.Downloaders;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;
using Wilgysef.Stalk.Core.Shared.StringFormatters;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Wilgysef.Stalk.Core.Downloaders;

public class DefaultDownloader : IDefaultDownloader
{
    public int DownloadBufferSize { get; set; } = 4 * 1024;

    public string Name => "Default";

    public ILogger? Logger { get; set; }

    private readonly IFileService _fileService;
    private readonly IStringFormatter _stringFormatter;
    private readonly IFilenameSlugSelector _filenameSlugSelector;
    private readonly HttpClient _httpClient;

    public DefaultDownloader(
        IFileService fileService,
        IStringFormatter stringFormatter,
        IFilenameSlugSelector filenameSlugSelector,
        HttpClient httpClient)
    {
        _fileService = fileService;
        _stringFormatter = stringFormatter;
        _filenameSlugSelector = filenameSlugSelector;
        _httpClient = httpClient;
    }

    public bool CanDownload(Uri uri)
    {
        return true;
    }

    public async IAsyncEnumerable<DownloadResult> DownloadAsync(
        Uri uri,
        string filenameTemplate,
        string itemId,
        string itemData,
        string metadataFilenameTemplate,
        IMetadataObject metadata,
        CancellationToken cancellationToken = default)
    {
        var downloadFileResult = await SaveFileAsync(
            uri,
            filenameTemplate,
            metadata,
            cancellationToken);

        var metadataConsts = new MetadataObjectConsts(metadata.KeySeparator);
        metadata.TryAddValue(metadataConsts.FileFilenameTemplateKey, filenameTemplate);
        metadata.TryAddValue(metadataConsts.MetadataFilenameTemplateKey, metadataFilenameTemplate);
        metadata.TryAddValue(metadataConsts.OriginItemIdKey, itemId);
        metadata.TryAddValue(metadataConsts.OriginUriKey, uri.ToString());
        metadata.TryAddValue(metadataConsts.RetrievedKey, DateTime.Now);

        metadata.TryAddValue(metadataConsts.FileSizeKey, downloadFileResult.FileSize);
        if (downloadFileResult.Hash != null)
        {
            metadata.TryAddValue(metadataConsts.FileHashKey, downloadFileResult.Hash);
            metadata.TryAddValue(metadataConsts.FileHashAlgorithmKey, downloadFileResult.HashName);
        }

        var metadataFilename = await SaveMetadataAsync(
            metadataFilenameTemplate,
            metadata.Dictionary,
            cancellationToken);

        yield return new DownloadResult(
            downloadFileResult.Filename,
            uri,
            itemId,
            itemData: itemData,
            metadataPath: metadataFilename,
            metadata: metadata);
    }

    protected async Task<DownloadFileResult> SaveFileAsync(
        Uri uri,
        string filenameTemplate,
        IMetadataObject metadata,
        CancellationToken cancellationToken = default)
    {
        var filenameSlug = _filenameSlugSelector.GetFilenameSlugByPlatform();
        var filename = filenameSlug.SlugifyPath(
            _stringFormatter.Format(filenameTemplate, metadata.Dictionary));

        var response = await _httpClient.GetAsync(uri, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var fileStream = _fileService.Open(filename, FileMode.CreateNew);

        var hashName = "SHA256";
        var result = await SaveStreamAsync(
            stream,
            fileStream,
            null,
            HashAlgorithm.Create(hashName),
            cancellationToken);
        result.Filename = filename;
        result.HashName = hashName;

        return result;
    }

    protected async Task<DownloadFileResult> SaveStreamAsync(
        Stream stream,
        Stream output,
        byte[]? buffer = null,
        HashAlgorithm? hashAlgorithm = null,
        CancellationToken cancellationToken = default)
    {
        long fileSize = 0;
        buffer ??= new byte[DownloadBufferSize];

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var bytesRead = await stream.ReadAsync(buffer, cancellationToken);
            fileSize += bytesRead;

            cancellationToken.ThrowIfCancellationRequested();
            await output.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);

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
            null,
            fileSize,
            hashAlgorithm?.Hash != null
                ? Convert.ToHexString(hashAlgorithm.Hash).ToLower()
                : null,
            null);
    }

    protected Task<string?> SaveMetadataAsync(
        string? metadataFilenameTemplate,
        IDictionary<string, object> metadata,
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

            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            serializer.Serialize(writer, metadata);
        }
        catch (IOException exception)
        {
            Logger?.LogError(exception, "Could not write metadata to {PATH}", metadataFilename);
        }

        return Task.FromResult<string?>(metadataFilename);
    }
}
