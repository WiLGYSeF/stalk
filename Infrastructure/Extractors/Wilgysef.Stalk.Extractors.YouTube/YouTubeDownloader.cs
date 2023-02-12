﻿using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Wilgysef.Stalk.Core.Shared.FilenameSlugs;
using Wilgysef.Stalk.Core.Shared.MetadataSerializers;
using Wilgysef.Stalk.Core.Shared.ProcessServices;
using Wilgysef.Stalk.Core.Shared.StringFormatters;
using Wilgysef.Stalk.Extractors.YoutubeDl.Core;

namespace Wilgysef.Stalk.Extractors.YouTube;

public class YouTubeDownloader : YoutubeDlDownloaderBase
{
    public override string Name => "YouTube";

    protected override Regex OutputOutputRegex { get; } = new($@"\[Merger\] Merging formats into \""(?<{YoutubeDlRunner.OutputOutputRegexGroup}>.*)\""$", RegexOptions.Compiled);

    public YouTubeDownloader(
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
            httpClient) { }

    public override bool CanDownload(Uri uri)
    {
        return Consts.VideoRegex.IsMatch(uri.GetLeftPart(UriPartial.Path));
    }

    protected override YoutubeDlConfig GetYoutubeDlConfig()
    {
        return GetYouTubeDownloaderConfig().ToYoutubeDlConfig();
    }

    protected override bool ShouldMoveInfoJsonToMetadata()
    {
        return GetYouTubeDownloaderConfig().MoveInfoJsonToMetadata;
    }

    private YouTubeDownloaderConfig GetYouTubeDownloaderConfig()
    {
        return new YouTubeDownloaderConfig(Config);
    }
}
