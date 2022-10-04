using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Wilgysef.Stalk.Core.Shared.CacheObjects;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Extractors;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;

namespace Wilgysef.Stalk.Extractors.YouTube;

public class YouTubeExtractor : IExtractor
{
    public string Name => "YouTube";

    public ILogger? Logger { get; set; }

    public ICacheObject<string, object?>? Cache { get; set; }

    public IDictionary<string, object?> Config { get; set; } = new Dictionary<string, object?>();

    public bool GetVideoMetadata { get; set; } = false;

    private const string YouTubeClientVersion = "2.20220929.09.00";

    private string[] ThumbnailUris = new string[]
    {
        "https://img.youtube.com/vi_webp/{0}/maxresdefault.webp",
        "https://img.youtube.com/vi/{0}/maxresdefault.jpg",
        "https://img.youtube.com/vi/{0}/sddefault.jpg",
        "https://img.youtube.com/vi/{0}/hqdefault.jpg",
        "https://img.youtube.com/vi/{0}/mqdefault.jpg",
        "https://img.youtube.com/vi/{0}/default.jpg",
    };

    private const int EmojiScaleWidth = 512;

    private const string UriPrefixRegex = @"^(?:https?://)?(?:(?:www\.|m\.)?youtube\.com|youtu\.be)";
    private static readonly Regex ChannelRegex = new(UriPrefixRegex + @"/(?<segment>c(?:hannel)?)/(?<channel>[A-Za-z0-9_-]+)", RegexOptions.Compiled);
    private static readonly Regex VideosRegex = new(UriPrefixRegex + @"/c(?:hannel)?/(?<channel>[A-Za-z0-9_-]+)/videos", RegexOptions.Compiled);
    private static readonly Regex PlaylistRegex = new(UriPrefixRegex + @"/playlist\?", RegexOptions.Compiled);
    private static readonly Regex VideoRegex = new(UriPrefixRegex + @"/watch\?", RegexOptions.Compiled);
    private static readonly Regex CommunityRegex = new(UriPrefixRegex + @"/c(?:hannel)?/(?<channel>[A-Za-z0-9_-]+)/community", RegexOptions.Compiled);

    private static readonly Regex CommunityImageRegex = new(@"^https://yt3\.ggpht\.com/(?<image>[A-Za-z0-9_-]+)=s(?<size>[0-9]+)", RegexOptions.Compiled);
    private static readonly Regex EmojiImageRegex = new(@"^https://yt3\.ggpht\.com/(?<image>[A-Za-z0-9_-]+)=w(?<width>[0-9]+)-h(?<height>[0-9]+)", RegexOptions.Compiled);

    private static readonly string[] MetadataChannelIdKeys = new string[] { "channel", "id" };
    private static readonly string[] MetadataChannelNameKeys = new string[] { "channel", "name" };
    private static readonly string[] MetadataVideoIdKeys = new string[] { "video", "id" };
    private static readonly string[] MetadataVideoTitleKeys = new string[] { "video", "title" };
    private static readonly string[] MetadataVideoDurationKeys = new string[] { "video", "duration" };
    private static readonly string[] MetadataVideoDurationSecondsKeys = new string[] { "video", "duration_seconds" };

    private HttpClient _httpClient;

    public YouTubeExtractor(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

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
        return GetExtractType(uri) switch
        {
            ExtractType.Channel => ExtractChannelAsync(uri, metadata, cancellationToken),
            ExtractType.Videos => ExtractVideosAsync(uri, metadata, cancellationToken),
            ExtractType.Playlist => ExtractPlaylistAsync(uri, metadata, cancellationToken),
            ExtractType.Video => ExtractVideoAsync(uri, metadata, cancellationToken),
            ExtractType.Community => ExtractCommunityAsync(uri, metadata, cancellationToken),
            _ => throw new ArgumentException("Cannot extract URI.", nameof(uri)),
        };
    }

    public void SetHttpClient(HttpClient client)
    {
        _httpClient = client;
    }

    private async IAsyncEnumerable<ExtractResult> ExtractChannelAsync(
        Uri uri,
        IMetadataObject metadata,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channelNameOrId = GetChannel(uri, out var shortChannel);

        await foreach (var result in ExtractVideosAsync(
            GetVideosUri(channelNameOrId, shortChannel),
            metadata,
            cancellationToken))
        {
            yield return result;
        }

        await foreach (var result in ExtractCommunityAsync(
            GetCommunityUri(channelNameOrId, shortChannel),
            metadata,
            cancellationToken))
        {
            yield return result;
        }
    }

    private async IAsyncEnumerable<ExtractResult> ExtractVideosAsync(
        Uri uri,
        IMetadataObject metadata,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channelNameOrId = GetChannel(uri, out var shortChannel);
        var channelId = shortChannel
            ? await GetChannelIdAsync(channelNameOrId, cancellationToken)
            : channelNameOrId;

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
            var playlistItems = json.SelectTokens("$..playlistVideoRenderer");
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var playlistItem in playlistItems)
            {
                if (GetVideoMetadata)
                {
                    var videoId = playlistItem["videoId"]!.ToString();
                    var channelId = playlistItem.SelectToken("$.shortBylineText..browseId")!.ToString();

                    yield return new ExtractResult(
                        new Uri($"https://www.youtube.com/watch?v={videoId}"),
                        $"{channelId}#video#{videoId}",
                        JobTaskType.Extract);
                }
                else
                {
                    foreach (var videoResult in ExtractVideoFromPlaylist(playlistItem, metadata.Copy()))
                    {
                        yield return videoResult;
                    }
                }
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

        yield return new ExtractResult(
            uri,
            $"{channelId}#video#{videoId}",
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
            var uri = string.Format(uriFormat, videoId);

            try
            {
                var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri), cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    continue;
                }

                if (published != null)
                {
                    metadata.SetByParts($"{channelId}#video#{published}_{videoId}_thumb", MetadataObjectConsts.Origin.ItemIdSeqKeys);
                }

                result = new ExtractResult(
                    new Uri(uri),
                    $"{channelId}#video#{videoId}_thumb",
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

    private async IAsyncEnumerable<ExtractResult> ExtractCommunityAsync(
        Uri uri,
        IMetadataObject metadata,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var json = await GetCommunityJsonAsync(uri, cancellationToken);
        var channelId = json.SelectToken("$..channelId")?.ToString()
            ?? json.SelectToken("$..authorEndpoint.browseEndpoint.browseId")!.ToString();

        var isSingle = uri.Query.Length > 0 && HttpUtility.ParseQueryString(uri.Query)["lb"] != null;

        while (true)
        {
            var communityPosts = json.SelectTokens("$..backstagePostRenderer");
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var communityPost in communityPosts)
            {
                foreach (var result in ExtractCommunity(communityPost, metadata.Copy()))
                {
                    yield return result;
                }
            }

            foreach (var result in ExtractEmojis(json, metadata, channelId))
            {
                yield return result;
            }

            if (isSingle && !communityPosts.Any())
            {
                break;
            }

            var continuationToken = json.SelectToken("$..continuationCommand.token")?.ToString();
            if (continuationToken == null)
            {
                break;
            }

            json = await GetCommunityJsonAsync(channelId, continuationToken, cancellationToken);
        }
    }

    private IEnumerable<ExtractResult> ExtractVideoFromPlaylist(JToken video, IMetadataObject metadata)
    {
        var videoId = video["videoId"]!.ToString();
        var channelId = video.SelectToken("$..shortBylineText..browseId")!.ToString();
        var channelName = video.SelectToken("$..shortBylineText..runs[0].text")!.ToString();

        var title = ConcatRuns(video.SelectToken("$.title.runs"));
        var lengthSeconds = video["lengthSeconds"]?.Value<int>();
        var lengthText = video.SelectToken("$.lengthText.simpleText")?.ToString();

        metadata.SetByParts(channelId, MetadataChannelIdKeys);
        metadata.SetByParts(channelName, MetadataChannelNameKeys);
        metadata.SetByParts(videoId, MetadataVideoIdKeys);
        metadata.SetByParts(title, MetadataVideoTitleKeys);
        metadata.SetByParts(lengthText, MetadataVideoDurationKeys);
        metadata.SetByParts(lengthSeconds, MetadataVideoDurationSecondsKeys);

        yield return new ExtractResult(
            new Uri($"https://www.youtube.com/watch?v={videoId}"),
            $"{channelId}#video#{videoId}",
            JobTaskType.Download,
            metadata: metadata);
    }

    private IEnumerable<ExtractResult> ExtractCommunity(JToken post, IMetadataObject metadata)
    {
        var postId = post["postId"]!.ToString();
        var channelId = post.SelectToken("$..authorEndpoint.browseEndpoint.browseId")!.ToString();
        var channelName = post.SelectToken("$..authorText.runs[*].text")!.ToString();

        metadata["post_id"] = postId;
        metadata.SetByParts(channelId, MetadataChannelIdKeys);
        metadata.SetByParts(channelName, MetadataChannelNameKeys);

        var publishedTime = post.SelectToken("$.publishedTimeText.runs[*].text");
        string? relativeDateTime = null;
        if (publishedTime != null)
        {
            relativeDateTime = GetRelativeDateTime(publishedTime.ToString());
            metadata["published"] = relativeDateTime;
            metadata["published_from"] = publishedTime.ToString() + $" from {DateTimeOffset.Now}";
        }

        metadata["votes"] = post["voteCount"]?["simpleText"]?.ToString();

        var commentsCount = post.SelectToken("$..replyButton.buttonRenderer.text.simpleText");
        if (commentsCount != null)
        {
            metadata["comments_count"] = commentsCount.ToString();
        }

        var imageResult = ExtractCommunityImage(post, metadata.Copy(), relativeDateTime);
        if (imageResult != null)
        {
            yield return imageResult;
        }

        var textResult = ExtractCommunityText(post, metadata.Copy(), relativeDateTime);
        if (textResult != null)
        {
            yield return textResult;
        }
    }

    private ExtractResult? ExtractCommunityText(JToken post, IMetadataObject metadata, string? relativeDateTime)
    {
        var postId = post["postId"]!.ToString();
        var channelId = post.SelectToken("$..authorEndpoint.browseEndpoint.browseId")!.ToString();
        var contextTextRuns = post["contentText"]?["runs"];
        if (contextTextRuns == null)
        {
            return null;
        }

        if (relativeDateTime != null)
        {
            metadata.SetByParts($"{channelId}#community#{relativeDateTime}_{postId}", MetadataObjectConsts.Origin.ItemIdSeqKeys);
        }

        var text = ConcatRuns(contextTextRuns);
        if (text == null)
        {
            return null;
        }

        metadata.SetByParts("txt", MetadataObjectConsts.File.ExtensionKeys);
        metadata.SetByParts($"https://www.youtube.com/channel/{channelId}/community?lb={postId}", MetadataObjectConsts.Origin.UriKeys);

        return new ExtractResult(
            Encoding.UTF8.GetBytes(text),
            $"{channelId}#community#{postId}",
            metadata: metadata);
    }

    private ExtractResult? ExtractCommunityImage(JToken post, IMetadataObject metadata, string? relativeDateTime)
    {
        var images = post.SelectToken("$..backstageImageRenderer.image.thumbnails");
        if (images == null)
        {
            return null;
        }
        var imageUrl = images.LastOrDefault()?["url"]?.ToString();
        if (imageUrl == null)
        {
            return null;
        }

        var postId = post["postId"]!.ToString();
        var channelId = post.SelectToken("$..authorEndpoint.browseEndpoint.browseId")!.ToString();

        metadata.SetByParts("png", MetadataObjectConsts.File.ExtensionKeys);

        if (relativeDateTime != null)
        {
            metadata.SetByParts($"{channelId}#community#{relativeDateTime}_{postId}_image", MetadataObjectConsts.Origin.ItemIdSeqKeys);
        }

        imageUrl = GetCommunityImageUrlFromThumbnail(imageUrl);

        return new ExtractResult(
            new Uri(imageUrl),
            $"{channelId}#community#{postId}_image",
            JobTaskType.Download,
            metadata: metadata);
    }

    private IEnumerable<ExtractResult> ExtractEmojis(JObject json, IMetadataObject metadata, string? channelId = null)
    {
        var customEmojis = json.SelectTokens("$..customEmojis[*]");
        if (customEmojis == null || !customEmojis.Any())
        {
            yield break;
        }

        foreach (var emoji in customEmojis)
        {
            var result = ExtractEmoji(emoji, metadata.Copy(), channelId);
            if (result != null)
            {
                yield return result;
            }
        }
    }

    private ExtractResult? ExtractEmoji(JToken emoji, IMetadataObject metadata, string? channelId = null)
    {
        var emojiIdParts = emoji["emojiId"]?.ToString().Split("/");
        if (emojiIdParts == null || emojiIdParts.Length != 2)
        {
            throw new ArgumentException("Invalid emoji Id.");
        }

        var emojiChannelId = emojiIdParts[0];
        var emojiSubId = emojiIdParts[1];

        if (channelId != null && emojiChannelId != channelId)
        {
            return null;
        }

        var url = emoji["image"]?["thumbnails"]?.LastOrDefault()?["url"]?.ToString();
        if (url == null)
        {
            throw new ArgumentException("Could not get emoji URL.");
        }

        metadata.SetByParts(emojiChannelId, MetadataChannelIdKeys);
        metadata["emoji_name"] = emoji.SelectToken("$..label")?.ToString();
        metadata.SetByParts("png", MetadataObjectConsts.File.ExtensionKeys);

        return new ExtractResult(
            new Uri(GetScaledEmojiImageUri(url)),
            $"{emojiChannelId}#emoji#{emojiSubId}",
            JobTaskType.Download,
            metadata: metadata);
    }

    private async Task<JObject> GetPlaylistJsonAsync(Uri uri, CancellationToken cancellationToken)
    {
        return await GetYtInitialData(uri, cancellationToken);
    }

    private async Task<JObject> GetCommunityJsonAsync(Uri uri, CancellationToken cancellationToken)
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

    private async Task<JObject> GetCommunityJsonAsync(
        string channelId,
        string continuationToken,
        CancellationToken cancellationToken)
    {
        return await GetBrowseJsonAsync(
            $"https://www.youtube.com/channel/{channelId}/community",
            continuationToken,
            cancellationToken);
    }

    private async Task<HtmlDocument> GetHtmlDocument(Uri uri, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(uri, cancellationToken);
        response.EnsureSuccessStatusCode();

        var doc = new HtmlDocument();
        doc.Load(response.Content.ReadAsStream(cancellationToken));
        return doc;
    }

    private async Task<JObject> GetYtInitialData(Uri uri, CancellationToken cancellationToken)
    {
        return GetYtInitialData(await GetHtmlDocument(uri, cancellationToken));
    }

    private static JObject GetYtInitialData(HtmlDocument doc)
    {
        var scripts = doc.DocumentNode.SelectNodes("//script");

        var prefix = "var ytInitialData = ";
        var initialData = scripts.Single(n => n.InnerHtml.TrimStart().StartsWith(prefix));
        var trimmedHtml = initialData.InnerHtml.Trim();
        var json = trimmedHtml.Substring(prefix.Length, trimmedHtml.Length - prefix.Length - 1);

        return JObject.Parse(json)
            ?? throw new ArgumentException("Could not get initial playlist data.", nameof(doc));
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

    private async Task<JObject> GetBrowseJsonAsync(
        string originalUrl,
        string continuationToken,
        CancellationToken cancellationToken)
    {
        var request = new
        {
            context = new
            {
                adSignalsInfo = new
                {
                    @params = Array.Empty<object>()
                },
                clickTracking = new { },
                client = new
                {
                    clientFormFactor = "UNKNOWN_FORM_FACTOR",
                    clientName = "WEB",
                    clientVersion = YouTubeClientVersion,
                    deviceMake = "",
                    deviceModel = "",
                    gl = "US",
                    hl = "en",
                    mainAppWebInfo = new
                    {
                        graftUrl = originalUrl,
                        isWebNativeShareAvailable = false,
                        webDisplayMode = "WEB_DISPLAY_MODE_BROWSER"
                    },
                    originalUrl = originalUrl,
                    platform = "DESKTOP",
                    userInterfaceTheme = "USER_INTERFACE_THEME_DARK",
                },
                request = new
                {
                    consistencyTokenJars = Array.Empty<object>(),
                    internalExperimentFlags = Array.Empty<object>(),
                    useSsl = true
                },
                user = new
                {
                    lockedSafetyMode = false
                }
            },
            continuation = continuationToken
        };

        var response = await _httpClient.PostAsJsonAsync(
            "https://www.youtube.com/youtubei/v1/browse?key=AIzaSyAO_FJ2SlqU8Q4STEHLGCilw_Y9_11qcW8&prettyPrint=false",
            request,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return JObject.Parse(content);
    }

    private async Task<string> GetChannelIdAsync(string channelName, CancellationToken cancellationToken)
    {
        // TODO: cache?

        var response = await _httpClient.GetAsync(GetChannelUriPrefix(channelName, true), cancellationToken);
        response.EnsureSuccessStatusCode();

        var doc = new HtmlDocument();
        doc.Load(response.Content.ReadAsStream(cancellationToken));

        var metaChannelId = doc.DocumentNode.SelectSingleNode("//meta[@itemprop=\"channelId\"]");
        return metaChannelId.Attributes["content"].Value;
    }

    private static string GetCommunityImageUrlFromThumbnail(string url)
    {
        var match = CommunityImageRegex.Match(url);
        return match.Success
            ? $"https://yt3.ggpht.com/{match.Groups["image"].Value}=s{match.Groups["size"].Value}-nd-v1"
            : url;
    }

    private static string GetScaledEmojiImageUri(string url)
    {
        var match = EmojiImageRegex.Match(url);
        if (!match.Success)
        {
            return url;
        }

        if (!int.TryParse(match.Groups["width"].Value, out var width)
            || !int.TryParse(match.Groups["height"].Value, out var height))
        {
            return url;
        }

        if (width < EmojiScaleWidth)
        {
            var mult = (float)EmojiScaleWidth / width;
            width = EmojiScaleWidth;
            height = (int)(height * mult);
        }

        return $"https://yt3.ggpht.com/{match.Groups["image"].Value}=w{width}-h{height}-c-k-nd";
    }

    private static Uri GetVideosUri(string channel, bool shortChannel = false)
    {
        return new Uri(GetChannelUriPrefix(channel, shortChannel) + "videos");
    }

    private static Uri GetVideosPlaylistUri(string channel)
    {
        if (!channel.StartsWith("UC") || channel.Length < 20)
        {
            throw new ArgumentException("Invalid channel format.", nameof(channel));
        }

        var channelPlaylist = "UU" + channel[2..];
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
        var match = ChannelRegex.Match(uri.AbsoluteUri);
        shortChannel = match.Groups["segment"].Value == "c";
        return match.Groups["channel"].Value;
    }

    private static ExtractType? GetExtractType(Uri uri)
    {
        var absoluteUri = uri.AbsoluteUri;
        if (VideosRegex.IsMatch(absoluteUri))
        {
            return ExtractType.Videos;
        }
        if (CommunityRegex.IsMatch(absoluteUri))
        {
            return ExtractType.Community;
        }
        if (ChannelRegex.IsMatch(absoluteUri))
        {
            return ExtractType.Channel;
        }
        if (PlaylistRegex.IsMatch(absoluteUri))
        {
            return ExtractType.Playlist;
        }
        if (VideoRegex.IsMatch(absoluteUri))
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

    private static DateTime? GetDateTime(string? dateTime)
    {
        if (dateTime == null)
        {
            return null;
        }

        var streamedLiveOn = "Streamed live on ";
        if (dateTime.StartsWith(streamedLiveOn))
        {
            dateTime = dateTime[streamedLiveOn.Length..];
        }

        var success = DateTime.TryParse(dateTime, out var result);
        if (success)
        {
            return result;
        }

        var streamed = "Streamed ";
        if (dateTime.StartsWith(streamed))
        {
            dateTime = dateTime[streamed.Length..];
        }

        var relative = GetRelativeDateTime(dateTime);
        return relative != null ? DateTime.Parse(relative) : null;
    }

    private static string? GetRelativeDateTime(string relative, DateTime? dateTime = null)
    {
        var split = relative.Split(" ");
        if (split.Length != 3 || !int.TryParse(split[0], out var number) || split[2] != "ago")
        {
            return null;
        }

        DateTime relativeTime;
        var span = split[1];
        dateTime ??= DateTime.Now;

        if (span.StartsWith("second"))
        {
            relativeTime = dateTime.Value.AddSeconds(-number);
        }
        else if (span.StartsWith("minute"))
        {
            relativeTime = dateTime.Value.AddMinutes(-number);
        }
        else if (span.StartsWith("hour"))
        {
            relativeTime = dateTime.Value.AddHours(-number);
        }
        else if (span.StartsWith("day"))
        {
            relativeTime = dateTime.Value.AddDays(-number);
        }
        else if (span.StartsWith("week"))
        {
            relativeTime = dateTime.Value.AddDays(-number * 7);
        }
        else if (span.StartsWith("month"))
        {
            relativeTime = dateTime.Value.AddMonths(-number);
        }
        else if (span.StartsWith("year"))
        {
            relativeTime = dateTime.Value.AddYears(-number);
        }
        else
        {
            return null;
        }

        return relativeTime.ToString("yyyyMMdd");
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
