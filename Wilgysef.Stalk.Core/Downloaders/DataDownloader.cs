using System.IO.Abstractions;
using System.Web;
using Wilgysef.Stalk.Core.Shared.Downloaders;
using Wilgysef.Stalk.Core.Shared.FilenameSlugs;
using Wilgysef.Stalk.Core.Shared.MetadataSerializers;
using Wilgysef.Stalk.Core.Shared.StringFormatters;

namespace Wilgysef.Stalk.Core.Downloaders;

public class DataDownloader : DownloaderBase
{
    public override string Name => "Data";

    public DataDownloader(
        IFileSystem fileSystem,
        IStringFormatter stringFormatter,
        IFilenameSlugSelector filenameSlugSelector,
        IMetadataSerializer metadataSerializer,
        HttpClient httpClient)
        : base(
            fileSystem,
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
        if (uri.OriginalString == "data:" && requestData?.Data != null)
        {
            return Task.FromResult<Stream>(new MemoryStream(requestData.Data));
        }

        return Task.FromResult(GetStreamFromDataUri(uri.AbsoluteUri));
    }

    private Stream GetStreamFromDataUri(string uri)
    {
        if (!uri.StartsWith("data:"))
        {
            throw new ArgumentException("Invalid data URI.", nameof(uri));
        }

        var offset = 5;
        for (; offset < uri.Length; offset++)
        {
            if (uri[offset] == ';' || uri[offset] == ',')
            {
                break;
            }
        }

        var isBase64 = false;

        if (uri[offset] == ';')
        {
            if (!uri[offset..].StartsWith(";base64,"))
            {
                throw new ArgumentException("Unknown encoding.", nameof(uri));
            }
            isBase64 = true;
            offset += 8;
        }
        else
        {
            offset++;
        }

        var data = uri[offset..];
        return new MemoryStream(isBase64
            ? Convert.FromBase64String(data)
            : HttpUtility.UrlDecodeToBytes(data));
    }
}
