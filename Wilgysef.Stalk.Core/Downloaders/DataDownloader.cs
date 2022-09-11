using Wilgysef.Stalk.Core.FilenameSlugs;
using Wilgysef.Stalk.Core.FileServices;
using Wilgysef.Stalk.Core.Shared.StringFormatters;

namespace Wilgysef.Stalk.Core.Downloaders;

public class DataDownloader : DownloaderBase
{
    public DataDownloader(
        IFileService fileService,
        IStringFormatter stringFormatter,
        IFilenameSlugSelector filenameSlugSelector,
        HttpClient httpClient)
        : base(
            fileService,
            stringFormatter,
            filenameSlugSelector,
            httpClient) { }

    public override bool CanDownload(Uri uri)
    {
        return uri.Scheme == "data";
    }

    protected override Task<Stream> GetFileStreamAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        var absoluteUri = uri.AbsoluteUri;
        var base64Index = absoluteUri.IndexOf("base64,");
        return Task.FromResult<Stream>(new MemoryStream(Convert.FromBase64String(absoluteUri.Substring(base64Index + "base64,".Length))));
    }
}
