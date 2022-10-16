using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Web;
using Wilgysef.Stalk.Core.Shared.CacheObjects;
using Wilgysef.Stalk.Core.Shared.DateTimeProviders;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Extensions;
using Wilgysef.Stalk.Core.Shared.Extractors;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;

namespace Wilgysef.Stalk.Extractors.YouTube;

public class YouTubeExtractor : YouTubeExtractorBase, IExtractor
{
    // TODO: live chat? (handled by youtube-dl already)
    // TODO: comments?

    public string Name => "YouTube";

    public ILogger? Logger { get; set; }

    public ICacheObject<string, object?>? Cache { get; set; }

    private readonly string[] ThumbnailUris = new string[]
    {
        "https://img.youtube.com/vi_webp/{0}/maxresdefault.webp",
        "https://img.youtube.com/vi/{0}/maxresdefault.jpg",
        "https://img.youtube.com/vi/{0}/sddefault.jpg",
        "https://img.youtube.com/vi/{0}/hqdefault.jpg",
        "https://img.youtube.com/vi/{0}/mqdefault.jpg",
        "https://img.youtube.com/vi/{0}/default.jpg",
    };

    /// <summary>
    /// Template string for file extension with youtube-dl.
    /// </summary>
    private const string YoutubeDlFileExtensionTemplate = "%(ext)s";

    public YouTubeExtractor(
        HttpClient httpClient,
        IDateTimeProvider dateTimeProvider)
        : base(httpClient, dateTimeProvider) { }

    public bool CanExtract(Uri uri)
    {
        return GetExtractType(uri) != null;
    }

    // TODO: should this be async?
    public IAsyncEnumerable<ExtractResult> ExtractAsync(
        Uri uri,
        string? itemData,
        IMetadataObject metadata,
        CancellationToken cancellationToken = default)
    {
        ExtractorConfig = new YouTubeExtractorConfig(Config);

        switch (GetExtractType(uri))
        {
            case ExtractType.Channel:
                return ExtractChannelAsync(uri, metadata, cancellationToken);
            case ExtractType.Videos:
                return ExtractVideosAsync(uri, null, metadata, cancellationToken);
            case ExtractType.Playlist:
                return ExtractPlaylistAsync(uri, metadata, cancellationToken);
            case ExtractType.Video:
                return ExtractVideoAsync(uri, metadata, cancellationToken);
            case ExtractType.Community:
                var communityExtractor = CreateCommunityExtractor();
                return communityExtractor.ExtractCommunityAsync(uri, metadata, cancellationToken);
            default:
                throw new ArgumentException("Cannot extract URI.", nameof(uri));
        }
    }

    public string? GetItemId(Uri uri)
    {
        var absoluteUri = uri.AbsoluteUri;

        if (Consts.VideoRegex.IsMatch(absoluteUri))
        {
            var query = HttpUtility.ParseQueryString(uri.Query);
            if (query.TryGetValue("v", out var videoId))
            {
                return videoId;
            }
        }

        if (Consts.CommunityRegex.IsMatch(absoluteUri))
        {
            var query = HttpUtility.ParseQueryString(uri.Query);
            if (query.TryGetValue("lb", out var postId))
            {
                return postId;
            }
        }

        var match = Consts.CommunityPostRegex.Match(absoluteUri);
        if (match.Success)
        {
            return match.Groups[Consts.CommunityPostRegexPostGroup].Value;
        }
        return null;
    }

    private async IAsyncEnumerable<ExtractResult> ExtractChannelAsync(
        Uri uri,
        IMetadataObject metadata,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var communityExtractor = CreateCommunityExtractor();

        var channelNameOrId = GetChannel(uri, out var shortChannel);
        var channelId = shortChannel
            ? await GetChannelIdAsync(channelNameOrId, cancellationToken)
            : channelNameOrId;

        await foreach (var result in ExtractVideosAsync(
            GetVideosUri(channelNameOrId, shortChannel),
            channelId,
            metadata,
            cancellationToken))
        {
            yield return result;
        }

        await foreach (var result in ExtractPlaylistAsync(
            GetVideosMembersOnlyUri(channelId),
            metadata,
            cancellationToken))
        {
            yield return result;
        }

        await foreach (var result in communityExtractor.ExtractCommunityAsync(
            GetCommunityUri(channelNameOrId, shortChannel),
            metadata,
            cancellationToken))
        {
            yield return result;
        }
    }

    private async IAsyncEnumerable<ExtractResult> ExtractVideosAsync(
        Uri uri,
        string? channelId,
        IMetadataObject metadata,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (channelId == null)
        {
            var channelNameOrId = GetChannel(uri, out var shortChannel);
            channelId = shortChannel
                ? await GetChannelIdAsync(channelNameOrId, cancellationToken)
                : channelNameOrId;
        }

        await foreach (var result in ExtractPlaylistAsync(
            GetVideosPlaylistUri(channelId),
            metadata,
            cancellationToken))
        {
            yield return result;
        }
    }

    private async IAsyncEnumerable<ExtractResult> ExtractPlaylistAsync(
        Uri uri,
        IMetadataObject metadata,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var json = await GetPlaylistJsonAsync(uri, cancellationToken);
        var playlistId = json.SelectToken("$..playlistVideoListRenderer.playlistId")!.ToString();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var playlistItems = json.SelectTokens("$..playlistVideoRenderer");

            foreach (var playlistItem in playlistItems)
            {
                var videoId = playlistItem["videoId"]!.ToString();
                var channelId = playlistItem.SelectToken("$.shortBylineText..browseId")!.ToString();

                yield return new ExtractResult(
                    $"https://www.youtube.com/watch?v={videoId}",
                    videoId,
                    JobTaskType.Extract);
            }

            var continuationToken = json.SelectToken("$..continuationCommand.token")?.ToString();
            if (continuationToken == null)
            {
                break;
            }

            json = await GetPlaylistJsonAsync(playlistId, continuationToken, cancellationToken);
        }
    }

    private async IAsyncEnumerable<ExtractResult> ExtractVideoAsync(
        Uri uri,
        IMetadataObject metadata,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var doc = await GetHtmlDocument(uri, cancellationToken);
        var initialData = GetYtInitialData(doc);
        var playerResponse = GetYtInitialPlayerResponse(doc);

        var channelId = initialData.SelectToken("$..videoOwnerRenderer.title.runs[0]..browseId")!.ToString();
        var channelName = initialData.SelectToken("$..videoOwnerRenderer.title.runs[0].text")?.ToString();
        var videoId = initialData.SelectTokens("$..topLevelButtons..watchEndpoint.videoId").First().ToString();
        var title = ConcatRuns(initialData.SelectToken("$..videoPrimaryInfoRenderer.title.runs"));
        var date = GetDateTime(initialData.SelectToken("$..dateText.simpleText")?.ToString());
        var description = ConcatRuns(initialData.SelectToken("$..description.runs"));
        var viewCount = initialData.SelectToken("$..viewCount.simpleText")!.ToString();
        var commentCount = initialData.SelectTokens("$..engagementPanels[*].engagementPanelSectionListRenderer")
            .FirstOrDefault(t => t["panelIdentifier"]?.ToString() == "comment-item-section")
            ?.SelectToken("$..contextualInfo.runs[*].text")
            ?.ToString();

        var videoActionsTopLevelButtonLabels = initialData.SelectTokens("$..videoActions..topLevelButtons[*]..label");
        var likeCount = videoActionsTopLevelButtonLabels.FirstOrDefault(t => t.ToString().EndsWith(" likes"))?.ToString();

        var videoDuration = GetApproximateDuration(playerResponse);

        var published = date?.ToString("yyyyMMdd");

        metadata.SetByParts(channelId, MetadataChannelIdKeys);
        metadata.SetByParts(channelName, MetadataChannelNameKeys);
        metadata.SetByParts(videoId, MetadataVideoIdKeys);
        metadata.SetByParts(title, MetadataVideoTitleKeys);
        metadata["published"] = published;
        metadata.SetByParts(TimeSpanToString(videoDuration), MetadataVideoDurationKeys);
        metadata.SetByParts(videoDuration?.TotalSeconds, MetadataVideoDurationSecondsKeys);
        metadata.SetByParts(description, "video", "description");
        metadata.SetByParts(viewCount, "video", "view_count");
        metadata.SetByParts(likeCount, "video", "like_count");
        metadata.SetByParts(commentCount, "video", "comment_count");

        var thumbnailResult = await ExtractThumbnailAsync(
            channelId,
            videoId,
            published,
            metadata.Copy(),
            cancellationToken);
        if (thumbnailResult != null)
        {
            yield return thumbnailResult;
        }

        if (published != null)
        {
            metadata.SetByParts($"{channelId}#video#{published}_{videoId}", MetadataObjectConsts.Origin.ItemIdSeqKeys);
        }

        metadata.SetByParts(YoutubeDlFileExtensionTemplate, MetadataObjectConsts.File.ExtensionKeys);

        yield return new ExtractResult(
            uri,
            videoId,
            JobTaskType.Download,
            metadata: metadata);
    }

    private async Task<ExtractResult?> ExtractThumbnailAsync(
        string channelId,
        string videoId,
        string? published,
        IMetadataObject metadata,
        CancellationToken cancellationToken)
    {
        ExtractResult? result = null;

        foreach (var uriFormat in ThumbnailUris)
        {
            if (!ExtractorConfig.UseWebpThumbnails && uriFormat.EndsWith(".webp"))
            {
                continue;
            }

            var uriString = string.Format(uriFormat, videoId);

            try
            {
                var request = ConfigureRequest(new HttpRequestMessage(HttpMethod.Head, uriString));
                var response = await HttpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    continue;
                }

                if (published != null)
                {
                    metadata.SetByParts($"{channelId}#video#{published}_{videoId}#thumb", MetadataObjectConsts.Origin.ItemIdSeqKeys);
                }

                var uri = new Uri(uriString);
                metadata.SetByParts(GetExtensionFromUri(uri), MetadataObjectConsts.File.ExtensionKeys);

                result = new ExtractResult(
                    uri,
                    $"{videoId}#thumb",
                    JobTaskType.Download,
                    metadata: metadata);
                break;
            }
            catch { }
        }

        if (result == null)
        {
            Logger?.LogError("YouTube: Could not get thumbnail for {VideoId}", videoId);
        }
        return result;
    }

    private async Task<JObject> GetPlaylistJsonAsync(Uri uri, CancellationToken cancellationToken)
    {
        return await GetYtInitialData(uri, cancellationToken);
    }

    private async Task<JObject> GetPlaylistJsonAsync(
        string playlistId,
        string continuationToken,
        CancellationToken cancellationToken)
    {
        return await GetBrowseJsonAsync(
            $"https://www.youtube.com/playlist?list={playlistId}",
            continuationToken,
            cancellationToken);
    }

    private static JObject GetYtInitialPlayerResponse(HtmlDocument doc)
    {
        var scripts = doc.DocumentNode.SelectNodes("//script");

        var prefix = "var ytInitialPlayerResponse = ";
        var initialData = scripts.Single(n => n.InnerHtml.TrimStart().StartsWith(prefix));
        var trimmedHtml = initialData.InnerHtml.Trim();
        var jsonWithJavascript = trimmedHtml[prefix.Length..];

        var reader = new JsonTextReader(new StringReader(jsonWithJavascript))
        {
            SupportMultipleContent = true
        };
        reader.Read();

        return JObject.Load(reader);
    }

    private async Task<string> GetChannelIdAsync(string channelName, CancellationToken cancellationToken)
    {
        // TODO: cache?
        var request = ConfigureRequest(new HttpRequestMessage(HttpMethod.Get, GetChannelUriPrefix(channelName, true)));
        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var doc = new HtmlDocument();
        doc.Load(response.Content.ReadAsStream(cancellationToken));

        var metaChannelId = doc.DocumentNode.SelectSingleNode("//meta[@itemprop=\"channelId\"]");
        return metaChannelId.Attributes["content"].Value;
    }

    private YouTubeCommunityExtractor CreateCommunityExtractor()
    {
        return new YouTubeCommunityExtractor(HttpClient, DateTimeProvider, ExtractorConfig, Logger, Cache);
    }

    private static Uri GetVideosUri(string channel, bool shortChannel = false)
    {
        return new Uri(GetChannelUriPrefix(channel, shortChannel) + "videos");
    }

    private static Uri GetVideosMembersOnlyUri(string channelId)
    {
        if (!channelId.StartsWith("UC") || channelId.Length < 20)
        {
            throw new ArgumentException("Invalid channel format.", nameof(channelId));
        }

        var channelPlaylist = "UUMO" + channelId[2..];
        return new Uri($"https://www.youtube.com/playlist?list={channelPlaylist}");
    }

    private static Uri GetVideosPlaylistUri(string channelId)
    {
        if (!channelId.StartsWith("UC") || channelId.Length < 20)
        {
            throw new ArgumentException("Invalid channel format.", nameof(channelId));
        }

        var channelPlaylist = "UU" + channelId[2..];
        return new Uri($"https://www.youtube.com/playlist?list={channelPlaylist}");
    }

    private static Uri GetCommunityUri(string channel, bool shortChannel = false)
    {
        return new Uri(GetChannelUriPrefix(channel, shortChannel) + "community");
    }

    private static string GetChannelUriPrefix(string channel, bool shortChannel = false)
    {
        var channelPathSegment = shortChannel ? "c" : "channel";
        return $"https://www.youtube.com/{channelPathSegment}/{channel}/";
    }

    private static string GetChannel(Uri uri, out bool shortChannel)
    {
        var match = Consts.ChannelRegex.Match(uri.AbsoluteUri);
        shortChannel = match.Groups[Consts.ChannelRegexChannelSegmentGroup].Value == "c";
        return match.Groups[Consts.ChannelRegexChannelGroup].Value;
    }

    private static ExtractType? GetExtractType(Uri uri)
    {
        var absoluteUri = uri.AbsoluteUri;
        if (Consts.VideosRegex.IsMatch(absoluteUri))
        {
            return ExtractType.Videos;
        }
        if (YouTubeCommunityExtractor.IsCommunityExtractType(uri))
        {
            return ExtractType.Community;
        }
        if (Consts.ChannelRegex.IsMatch(absoluteUri))
        {
            return ExtractType.Channel;
        }
        if (Consts.PlaylistRegex.IsMatch(absoluteUri))
        {
            return ExtractType.Playlist;
        }
        if (Consts.VideoRegex.IsMatch(absoluteUri))
        {
            return ExtractType.Video;
        }
        return null;
    }

    private static string? ConcatRuns(JToken? runs)
    {
        if (runs == null)
        {
            return null;
        }

        var textBuilder = new StringBuilder();
        foreach (var content in runs)
        {
            if (content["text"] != null)
            {
                textBuilder.Append(content["text"]!.ToString());
            }
        }
        return textBuilder.ToString();
    }

    private static TimeSpan? GetApproximateDuration(JToken playerResponse)
    {
        var approxDuration = playerResponse.SelectTokens("$..approxDurationMs")
            .Select(t => t.ToString())
            .GroupBy(d => d)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()
            ?.Key;

        return long.TryParse(approxDuration, out var result)
            ? TimeSpan.FromMilliseconds(result)
            : null;
    }

    private static string? TimeSpanToString(TimeSpan? timeSpan)
    {
        return timeSpan.HasValue
            ? timeSpan.Value.Hours > 0
                ? timeSpan.Value.ToString(@"hh\:mm\:ss")
                : timeSpan.Value.ToString(@"mm\:ss")
            : null;
    }

    private static string? GetExtensionFromUri(Uri uri)
    {
        var extension = Path.GetExtension(uri.AbsolutePath);
        return extension.Length > 0 && extension[0] == '.'
            ? extension[1..]
            : extension;
    }

    private enum ExtractType
    {
        Channel,
        Videos,
        Playlist,
        Video,
        Community,
    }
}
