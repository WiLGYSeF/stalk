using Wilgysef.Stalk.Core.Shared.Downloaders;
using Wilgysef.Stalk.Core.Shared.FilenameSlugs;
using Wilgysef.Stalk.Core.Shared.FileServices;
using Wilgysef.Stalk.Core.Shared.MetadataSerializers;
using Wilgysef.Stalk.Core.Shared.StringFormatters;

namespace Wilgysef.Stalk.Core.Downloaders;

public class DataDownloader : DownloaderBase
{
    public DataDownloader(
        IFileService fileService,
        IStringFormatter stringFormatter,
        IFilenameSlugSelector filenameSlugSelector,
        IMetadataSerializer metadataSerializer,
        HttpClient httpClient)
        : base(
            fileService,
            stringFormatter,
            filenameSlugSelector,
            metadataSerializer,
            httpClient)
    { }

    public override bool CanDownload(Uri uri)
    {
        return uri.Scheme == "data";
    }

    protected override Task<Stream> GetFileStreamAsync(
        Uri uri,
        DownloadRequestData? requestData = null,
        CancellationToken cancellationToken = default)
    {
        var absoluteUri = uri.AbsoluteUri;
        var base64Index = absoluteUri.IndexOf("base64,");
        return Task.FromResult<Stream>(new MemoryStream(Convert.FromBase64String(absoluteUri.Substring(base64Index + "base64,".Length))));
    }
}
