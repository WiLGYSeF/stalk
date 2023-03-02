using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Wilgysef.Stalk.Core.Shared.CacheObjects;
using Wilgysef.Stalk.Core.Shared.DateTimeProviders;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Extractors;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;

namespace Wilgysef.Stalk.Extractors.YouTube;

public class YouTubeExtractor : YouTubeExtractorBase, IExtractor
{
    // TODO: live chat? (handled by youtube-dl already)
    // TODO: comments?

    public string Name => "YouTube";

    public string Version => "20230218";

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

    private static readonly string[] MetadataVideoIdKeys = new string[] { "video", "id" };
    private static readonly string[] MetadataVideoTitleKeys = new string[] { "video", "title" };
    private static readonly string[] MetadataVideoPublishedKeys = new string[] { "video", "published" };
    private static readonly string[] MetadataVideoDurationKeys = new string[] { "video", "duration" };
    private static readonly string[] MetadataVideoDurationSecondsKeys = new string[] { "video", "duration_seconds" };
    private static readonly string[] MetadataVideoViewCountKeys = new string[] { "video", "view_count" };
    private static readonly string[] MetadataVideoDescriptionKeys = new string[] { "video", "description" };
    private static readonly string[] MetadataVideoLikeCountKeys = new string[] { "video", "like_count" };
    private static readonly string[] MetadataVideoCommentCountKeys = new string[] { "video", "comment_count" };
    private static readonly string[] MetadataVideoIsMembersOnlyKeys = new string[] { "video", "is_members_only" };

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
        return YouTubeUri.TryGetUri(uri, out _);
    }

    public IAsyncEnumerable<ExtractResult> ExtractAsync(
        Uri uri,
        string? itemData,
        IMetadataObject metadata,
        CancellationToken cancellationToken = default)
    {
        ExtractorConfig = new YouTubeExtractorConfig(Config);

        var youTubeUri = new YouTubeUri(uri);
        var communityExtractor = CreateCommunityExtractor();

        switch (youTubeUri.Type)
        {
            case YouTubeUriType.Featured:
                return ExtractChannelAsync(youTubeUri, metadata, cancellationToken);
            case YouTubeUriType.Videos:
                return ExtractVideosAsync(youTubeUri, metadata, YouTubeUri.GetChannelVideosPlaylistUri, cancellationToken);
            case YouTubeUriType.Shorts:
                return ExtractVideosAsync(youTubeUri, metadata, YouTubeUri.GetChannelShortsPlaylistUri, cancellationToken);
            case YouTubeUriType.Livestreams:
                return ExtractVideosAsync(youTubeUri, metadata, YouTubeUri.GetChannelLivestreamsPlaylistUri, cancellationToken);
            case YouTubeUriType.Playlist:
                return ExtractPlaylistAsync(youTubeUri.Uri, metadata, cancellationToken);
            case YouTubeUriType.Community:
            case YouTubeUriType.CommunityPost:
                return communityExtractor.ExtractCommunityAsync(youTubeUri, metadata, cancellationToken);
            case YouTubeUriType.Membership:
                var membershipExtractor = CreateMembershipExtractor(communityExtractor);
                return membershipExtractor.ExtractMembershipAsync(youTubeUri.Uri, metadata, cancellationToken);
            case YouTubeUriType.Video:
                return ExtractVideoAsync(youTubeUri.Uri, metadata, cancellationToken);
            case YouTubeUriType.Short:
                return ExtractShortAsync(youTubeUri, metadata, cancellationToken);
            default:
                throw new ArgumentOutOfRangeException($"Unknown YouTube URI type: {youTubeUri.Type}");
        }
    }

    public string? GetItemId(Uri uri)
    {
        if (!YouTubeUri.TryGetUri(uri, out var youTubeUri))
        {
            return null;
        }

        switch (youTubeUri.Type)
        {
            case YouTubeUriType.Video:
            case YouTubeUriType.Short:
            case YouTubeUriType.CommunityPost:
                return youTubeUri.ItemId;
            default:
                return null;
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    private async IAsyncEnumerable<ExtractResult> ExtractChannelAsync(
        YouTubeUri uri,
        IMetadataObject metadata,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var communityExtractor = CreateCommunityExtractor();

        var channelId = await GetChannelIdAsync(uri, cancellationToken);

        await foreach (var result in ExtractVideosAsync(
            channelId,
            metadata,
            cancellationToken))
        {
            yield return result;
        }

        await foreach (var result in ExtractPlaylistAsync(
            YouTubeUri.GetChannelMembershipAllVideosPlaylistUri(channelId),
            metadata,
            cancellationToken))
        {
            yield return result;
        }

        await foreach (var result in communityExtractor.ExtractCommunityAsync(
            GetCommunityUri(uri),
            metadata,
            cancellationToken))
        {
            yield return result;
        }
    }

    private async IAsyncEnumerable<ExtractResult> ExtractVideosAsync(
        YouTubeUri uri,
        IMetadataObject metadata,
        Func<string, Uri> uriFactory,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channelId = await GetChannelIdAsync(uri, cancellationToken);
        await foreach (var result in ExtractPlaylistAsync(
            uriFactory(channelId),
            metadata,
            cancellationToken))
        {
            yield return result;
        }
    }

    private async IAsyncEnumerable<ExtractResult> ExtractVideosAsync(
        string channelId,
        IMetadataObject metadata,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var result in ExtractPlaylistAsync(
            YouTubeUri.GetChannelAllVideosPlaylistUri(channelId),
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
        metadata = metadata.Copy();

        var doc = await GetHtmlDocument(uri, cancellationToken);
        var initialData = GetYtInitialData(doc);
        var playerResponse = GetYtInitialPlayerResponse(doc);

        var channelId = GetMetadata<string>(metadata, initialData.SelectToken("$..videoOwnerRenderer.title.runs[0]..browseId"), MetadataChannelIdKeys)!;
        var channelName = GetMetadata<string>(metadata, initialData.SelectToken("$..videoOwnerRenderer.title.runs[0].text"), MetadataChannelNameKeys);

        var videoId = initialData.SelectToken("$..currentVideoEndpoint..videoId")!.ToString();

        var title = ConcatRuns(initialData.SelectToken("$..videoPrimaryInfoRenderer.title.runs"));
        var date = GetDateTime(initialData.SelectToken("$..dateText.simpleText")?.ToString());
        var description = ConcatRuns(initialData.SelectToken("$..description.runs"));
        var commentCount = initialData.SelectTokens("$..engagementPanels[*].engagementPanelSectionListRenderer")
            .FirstOrDefault(t => t["panelIdentifier"]?.ToString() == "comment-item-section")
            ?.SelectToken("$..contextualInfo.runs[*].text")
            ?.ToString();

        var videoActionsTopLevelButtonLabels = initialData.SelectTokens("$..videoActions..topLevelButtons[*]..label");
        var likeCount = videoActionsTopLevelButtonLabels.FirstOrDefault(t => t.ToString().EndsWith(" likes"))?.ToString();

        var videoDuration = GetApproximateDuration(playerResponse);

        var published = date?.ToString("yyyyMMdd");

        GetMetadata<string>(metadata, initialData.SelectToken("$..viewCount.simpleText"), MetadataVideoViewCountKeys);

        metadata[MetadataVideoIdKeys] = videoId;
        metadata[MetadataVideoTitleKeys] = title;
        metadata[MetadataVideoPublishedKeys] = published;
        metadata[MetadataVideoDurationKeys] = TimeSpanToString(videoDuration);
        metadata[MetadataVideoDurationSecondsKeys] = videoDuration?.TotalSeconds;
        metadata[MetadataVideoDescriptionKeys] = description;
        metadata[MetadataVideoLikeCountKeys] = likeCount;
        metadata[MetadataVideoCommentCountKeys] = commentCount;
        metadata[MetadataVideoIsMembersOnlyKeys] = initialData.SelectTokens("$..badges[*].metadataBadgeRenderer.icon.iconType").Any(t => t.ToString() == "SPONSORSHIP_STAR");

        var thumbnailResult = await ExtractThumbnailAsync(
            channelId,
            videoId,
            published,
            metadata,
            cancellationToken);
        if (thumbnailResult != null)
        {
            yield return thumbnailResult;
        }

        metadata[MetadataObjectConsts.Origin.ItemIdSeqKeys] = published != null
            ? $"{channelId}#video#{published}_{videoId}"
            : $"{channelId}#video#{videoId}";

        metadata[MetadataObjectConsts.File.ExtensionKeys] = YoutubeDlFileExtensionTemplate;

        yield return new ExtractResult(
            uri,
            videoId,
            JobTaskType.Download,
            metadata: metadata);
    }

    private IAsyncEnumerable<ExtractResult> ExtractShortAsync(
        YouTubeUri uri,
        IMetadataObject metadata,
        CancellationToken cancellationToken)
    {
        var shortId = uri.ItemId;
        metadata[MetadataObjectConsts.Origin.UriKeys] = uri.Uri.AbsoluteUri;
        return ExtractVideoAsync(new Uri($"https://www.youtube.com/watch?v={shortId}"), metadata, cancellationToken);
    }

    private async Task<ExtractResult?> ExtractThumbnailAsync(
        string channelId,
        string videoId,
        string? published,
        IMetadataObject metadata,
        CancellationToken cancellationToken)
    {
        metadata = metadata.Copy();

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

                metadata[MetadataObjectConsts.Origin.ItemIdSeqKeys] = published != null
                    ? $"{channelId}#video#{published}_{videoId}#thumb"
                    : $"{channelId}#video#{videoId}#thumb";

                var uri = new Uri(uriString);
                metadata[MetadataObjectConsts.File.ExtensionKeys] = GetExtensionFromUri(uri);

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

    private Task<string> GetChannelIdAsync(YouTubeUri uri, CancellationToken cancellationToken)
    {
        if (uri.HasChannelId)
        {
            if (uri.ChannelNameOrId == null)
            {
                throw new ArgumentException(null, nameof(uri));
            }

            return Task.FromResult(uri.ChannelNameOrId);
        }

        return GetAsync();

        async Task<string> GetAsync()
        {
            // TODO: cache?
            var request = ConfigureRequest(new HttpRequestMessage(HttpMethod.Get, uri.GetChannelUri()));
            var response = await HttpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var doc = new HtmlDocument();
            doc.Load(response.Content.ReadAsStream(cancellationToken));

            var metaChannelId = doc.DocumentNode.SelectSingleNode("//meta[@itemprop=\"channelId\"]");
            return metaChannelId.Attributes["content"].Value;
        }
    }

    private YouTubeCommunityExtractor CreateCommunityExtractor()
    {
        return new YouTubeCommunityExtractor(HttpClient, DateTimeProvider, ExtractorConfig);
    }

    private YouTubeMembershipExtractor CreateMembershipExtractor(YouTubeCommunityExtractor communityExtractor)
    {
        return new YouTubeMembershipExtractor(HttpClient, communityExtractor, DateTimeProvider, ExtractorConfig);
    }

    private static YouTubeUri GetCommunityUri(YouTubeUri uri)
    {
        return new YouTubeUri(new Uri(uri.GetChannelUri() + "community"));
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

        if (approxDuration == null)
        {
            return null;
        }

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
}
