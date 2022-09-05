using System.Security.Cryptography;
using Wilgysef.Stalk.Core.FilenameSlugs;
using Wilgysef.Stalk.Core.FileServices;
using Wilgysef.Stalk.Core.Shared.Downloaders;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;
using Wilgysef.Stalk.Core.Shared.StringFormatters;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Wilgysef.Stalk.Core.Downloaders;

internal class DefaultDownloader : IDefaultDownloader
{
    private const int DownloadBufferSize = 4 * 1024;

    public string Name => "Default";

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
        var metadataObjectConsts = new MetadataObjectConsts(metadata.KeySeparator);

        metadata.TryAddValue(metadataObjectConsts.FileFilenameTemplateKey, filenameTemplate);
        metadata.TryAddValue(metadataObjectConsts.MetadataFilenameTemplateKey, metadataFilenameTemplate);
        metadata.TryAddValue(metadataObjectConsts.OriginItemIdKey, itemId);
        metadata.TryAddValue(metadataObjectConsts.OriginUriKey, uri.ToString());
        metadata.TryAddValue(metadataObjectConsts.RetrievedKey, DateTime.Now);

        var filenameSlug = _filenameSlugSelector.GetFilenameSlugByPlatform();
        var filename = filenameSlug.SlugifyPath(
            _stringFormatter.Format(filenameTemplate, metadata.Dictionary));

        var response = await _httpClient.GetAsync(uri, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var fileStream = _fileService.Open(filename, FileMode.CreateNew);

        var hashName = "SHA256";
        var downloadFileResult = await SaveStreamAsync(
            stream,
            fileStream,
            null,
            HashAlgorithm.Create(hashName),
            cancellationToken);

        metadata.TryAddValue(metadataObjectConsts.FileSizeKey, downloadFileResult.FileSize);
        if (downloadFileResult.Hash != null)
        {
            metadata.TryAddValue(metadataObjectConsts.FileHashKey, downloadFileResult.Hash);
            metadata.TryAddValue(metadataObjectConsts.FileHashAlgorithmKey, hashName);
        }

        var metadataFilename = SaveMetadata(metadataFilenameTemplate, metadata.Dictionary);

        yield return new DownloadResult(
            filename,
            uri,
            itemId,
            itemData: itemData,
            metadataPath: metadataFilename,
            metadata: metadata);
    }

    private string? SaveMetadata(string? metadataFilenameTemplate, IDictionary<string, object> metadata)
    {
        if (metadataFilenameTemplate == null)
        {
            return null;
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
        catch (IOException) { }

        return metadataFilename;
    }

    private async Task<DownloadFileResult> SaveStreamAsync(
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
            fileSize,
            hashAlgorithm?.Hash != null
                ? Convert.ToHexString(hashAlgorithm.Hash).ToLower()
                : null);
    }
}
