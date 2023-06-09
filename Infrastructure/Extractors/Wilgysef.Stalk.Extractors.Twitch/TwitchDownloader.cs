using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Wilgysef.Stalk.Core.Shared.FilenameSlugs;
using Wilgysef.Stalk.Core.Shared.MetadataSerializers;
using Wilgysef.Stalk.Core.Shared.ProcessServices;
using Wilgysef.Stalk.Core.Shared.StringFormatters;
using Wilgysef.Stalk.Extractors.YoutubeDl.Core;

namespace Wilgysef.Stalk.Extractors.Twitch;

public class TwitchDownloader : YoutubeDlDownloaderBase
{
    public override string Name => "Twitch";

    public override string Version => "2022.10.29";

    protected override Regex OutputOutputRegex { get; } = new($@"\[download\] Destination: (?<{YoutubeDlRunner.OutputOutputRegexGroup}>.*\.mp4)$", RegexOptions.Compiled);

    public TwitchDownloader(
        IFileSystem fileSystem,
        IStringFormatter stringFormatter,
        IFilenameSlugSelector filenameSlugSelector,
        IMetadataSerializer metadataSerializer,
        IProcessService processService,
        HttpClient httpClient)
        : base(
            fileSystem,
            stringFormatter,
            filenameSlugSelector,
            metadataSerializer,
            processService,
            httpClient)
    { }

    public override bool CanDownload(Uri uri)
    {
        return Consts.VideoRegex.IsMatch(uri.GetLeftPart(UriPartial.Path));
    }

    protected override YoutubeDlConfig GetYoutubeDlConfig()
    {
        return GetTwitchDownloaderConfig().ToYoutubeDlConfig();
    }

    protected override bool ShouldMoveInfoJsonToMetadata()
    {
        return GetTwitchDownloaderConfig().MoveInfoJsonToMetadata;
    }

    private TwitchDownloaderConfig GetTwitchDownloaderConfig()
    {
        return new TwitchDownloaderConfig(Config);
    }
}
