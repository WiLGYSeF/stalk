using Wilgysef.Stalk.Core.FilenameSlugs;
using Wilgysef.Stalk.Core.FileServices;
using Wilgysef.Stalk.Core.MetadataObjects;
using Wilgysef.Stalk.Core.Shared.Downloaders;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;
using Wilgysef.Stalk.Core.Shared.StringFormatters;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Wilgysef.Stalk.Core.Downloaders;

internal class DefaultDownloader : IDefaultDownloader
{
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

        metadata.TryAddValue(metadataObjectConsts.FilenameTemplateKey, filenameTemplate);
        metadata.TryAddValue(metadataObjectConsts.MetadataFilenameTemplateKey, metadataFilenameTemplate);
        metadata.TryAddValue(metadataObjectConsts.OriginItemId, itemId);
        metadata.TryAddValue(metadataObjectConsts.OriginUri, uri);
        metadata.TryAddValue(metadataObjectConsts.RetrievedKey, DateTime.Now);

        var filenameSlug = _filenameSlugSelector.GetFilenameSlugByPlatform();
        var filename = filenameSlug.SlugifyPath(
            _stringFormatter.Format(filenameTemplate, metadata.Dictionary));

        // TODO: download file
        var a = await _httpClient.GetAsync("https://example.com", cancellationToken);

        var metadataFilename = SaveMetadata(metadataFilenameTemplate, metadata.Dictionary);

        await Task.Delay(1);

        yield return new DownloadResult(
            null,
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
}
