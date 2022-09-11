using Wilgysef.Stalk.Core.FilenameSlugs;
using Wilgysef.Stalk.Core.FileServices;
using Wilgysef.Stalk.Core.Shared.StringFormatters;

namespace Wilgysef.Stalk.Core.Downloaders;

public sealed class DefaultDownloader : DownloaderBase
{
    public DefaultDownloader(
        IFileService fileService,
        IStringFormatter stringFormatter,
        IFilenameSlugSelector filenameSlugSelector,
        HttpClient httpClient)
        : base(
            fileService,
            stringFormatter,
            filenameSlugSelector,
            httpClient) { }
}
