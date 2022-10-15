using System.IO.Abstractions;
using Wilgysef.Stalk.Core.Shared.Downloaders;
using Wilgysef.Stalk.Core.Shared.FilenameSlugs;
using Wilgysef.Stalk.Core.Shared.MetadataSerializers;
using Wilgysef.Stalk.Core.Shared.StringFormatters;

namespace Wilgysef.Stalk.Extractors.Twitch;

public class TwitchDownloader : DownloaderBase
{
    public override string Name => "Twitch";

    public TwitchDownloader(
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
    {

    }

    public override bool CanDownload(Uri uri)
    {
        return Consts.VideoRegex.IsMatch(uri.AbsoluteUri);
    }
}
