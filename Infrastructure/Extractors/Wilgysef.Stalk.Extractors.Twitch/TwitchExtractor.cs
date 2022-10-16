using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Dynamic;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Web;
using Wilgysef.Stalk.Core.Shared.CacheObjects;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Extensions;
using Wilgysef.Stalk.Core.Shared.Extractors;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;

namespace Wilgysef.Stalk.Extractors.Twitch;

public class TwitchExtractor : IExtractor
{
    public string Name => "Twitch";

    public ILogger? Logger { get; set; }

    public ICacheObject<string, object?>? Cache { get; set; }

    public IDictionary<string, object?> Config { get; set; } = new Dictionary<string, object?>();

    private static readonly string ClientIdHeader = "Client-Id";

    private const string ChannelRegex = @"(?<channel>[A-Za-z0-9_-]+)";
    private static readonly Regex ChannelUriRegex = new(Consts.UriPrefixRegex + $@"/{ChannelRegex}/?", RegexOptions.Compiled);
    private static readonly Regex VideosRegex = new(Consts.UriPrefixRegex + $@"/{ChannelRegex}/videos", RegexOptions.Compiled);
    private static readonly Regex ClipsRegex = new(Consts.UriPrefixRegex + $@"/{ChannelRegex}/clips", RegexOptions.Compiled);
    private static readonly Regex ClipRegex = new(Consts.UriPrefixRegex + $@"/{ChannelRegex}/clip/(?<clip>[A-Za-z0-9_-]+)", RegexOptions.Compiled);
    private static readonly Regex ClipAltRegex = new(@"(?:https?://)?clips\.twitch\.tv/(?<clip>[A-Za-z0-9_-]+)", RegexOptions.Compiled);
    private static readonly Regex AboutRegex = new(Consts.UriPrefixRegex + $@"/{ChannelRegex}/about/?", RegexOptions.Compiled);

    private static readonly string[] MetadataChannelIdKeys = new[] { "channel", "id" };
    private static readonly string[] MetadataChannelNameKeys = new[] { "channel", "name" };
    private static readonly string[] MetadataChannelLoginKeys = new[] { "channel", "login" };

    private static readonly string[] MetadataVideoIdKeys = new[] { "video", "id" };
    private static readonly string[] MetadataVideoLengthSecondsKeys = new[] { "video", "length_seconds" };
    private static readonly string[] MetadataVideoLengthKeys = new[] { "video", "length" };
    private static readonly string[] MetadataVideoPublishedAtKeys = new[] { "video", "published_at" };
    private static readonly string[] MetadataVideoTitleKeys = new[] { "video", "title" };
    private static readonly string[] MetadataVideoViewCountKeys = new[] { "video", "view_count" };
    private static readonly string[] MetadataVideoTagsKeys = new[] { "video", "tags" };
    private static readonly string[] MetadataGameIdKeys = new[] { "video", "game", "id" };
    private static readonly string[] MetadataGameNameKeys = new[] { "video", "game", "name" };
    private static readonly string[] MetadataGameBoxartUrlKeys = new[] { "video", "game", "boxart_url" };

    private static readonly string[] MetadataClipSlugKeys = new[] { "clip", "slug" };
    private static readonly string[] MetadataClipIdKeys = new[] { "clip", "id" };
    private static readonly string[] MetadataClipUrlKeys = new[] { "clip", "url" };
    private static readonly string[] MetadataClipTitleKeys = new[] { "clip", "title" };
    private static readonly string[] MetadataClipCreatedAtKeys = new[] { "clip", "created_at" };
    private static readonly string[] MetadataClipDurationKeys = new[] { "clip", "duration" };
    private static readonly string[] MetadataClipDurationSecondsKeys = new[] { "clip", "duration_seconds" };
    private static readonly string[] MetadataClipViewCountKeys = new[] { "clip", "view_count" };
    private static readonly string[] MetadataClipCuratorIdKeys = new[] { "clip", "curator", "id" };
    private static readonly string[] MetadataClipCuratorNameKeys = new[] { "clip", "curator", "name" };
    private static readonly string[] MetadataClipCuratorLoginKeys = new[] { "clip", "curator", "login" };
    private static readonly string[] MetadataClipGameIdKeys = new[] { "clip", "game", "id" };
    private static readonly string[] MetadataClipGameNameKeys = new[] { "clip", "game", "name" };

    private static readonly string[] MetadataEmotePriceKeys = new[] { "emote", "price" };
    private static readonly string[] MetadataEmoteTierKeys = new[] { "emote", "tier" };
    private static readonly string[] MetadataEmoteIdKeys = new[] { "emote", "id" };
    private static readonly string[] MetadataEmoteSetIdKeys = new[] { "emote", "set_id" };
    private static readonly string[] MetadataEmoteTokenKeys = new[] { "emote", "token" };
    private static readonly string[] MetadataEmoteAssetTypeKeys = new[] { "emote", "asset_type" };

    /// <summary>
    /// Template string for file extension with youtube-dl.
    /// </summary>
    private const string YoutubeDlFileExtensionTemplate = "%(ext)s";

    private HttpClient _httpClient;

    public TwitchExtractor(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public bool CanExtract(Uri uri)
    {
        return GetExtractType(uri) != null;
    }

    public IAsyncEnumerable<ExtractResult> ExtractAsync(Uri uri, string? itemData, IMetadataObject metadata, CancellationToken cancellationToken = default)
    {
        return GetExtractType(uri) switch
        {
            ExtractType.Channel => ExtractChannelAsync(uri, metadata, cancellationToken),
            ExtractType.Videos => ExtractVideosAsync(uri, metadata, cancellationToken),
            ExtractType.Video => ExtractVideoAsync(uri, metadata, cancellationToken),
            ExtractType.Clips => ExtractClipsAsync(uri, metadata, cancellationToken),
            ExtractType.Clip => ExtractClipAsync(uri, metadata, cancellationToken),
            ExtractType.About => ExtractAboutAsync(uri, metadata, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(uri)),
        };
    }

    public string? GetItemId(Uri uri)
    {
        var absoluteUri = uri.AbsoluteUri;

        if (Consts.VideoRegex.TryMatch(absoluteUri, out var videoMatch))
        {
            return videoMatch.Groups["video"].Value;
        }
        if (ClipRegex.TryMatch(absoluteUri, out var clipMatch))
        {
            return clipMatch.Groups["clip"].Value;
        }
        if (ClipAltRegex.TryMatch(absoluteUri, out var clipAltMatch))
        {
            return clipAltMatch.Groups["clip"].Value;
        }

        return null;
    }

    public void SetHttpClient(HttpClient client)
    {
        _httpClient = client;
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private static async IAsyncEnumerable<ExtractResult> ExtractChannelAsync(
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        Uri uri,
        IMetadataObject metadata,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var match = ChannelUriRegex.Match(uri.AbsoluteUri);
        var channelName = match.Groups["channel"].Value;

        yield return new ExtractResult(
            $"https://www.twitch.tv/{channelName}/videos",
            null,
            JobTaskType.Extract,
            metadata: metadata);

        yield return new ExtractResult(
            $"https://www.twitch.tv/{channelName}/clips",
            null,
            JobTaskType.Extract,
            metadata: metadata);

        yield return new ExtractResult(
            $"https://www.twitch.tv/{channelName}/about",
            null,
            JobTaskType.Extract,
            metadata: metadata);
    }

    private async IAsyncEnumerable<ExtractResult> ExtractVideosAsync(
        Uri uri,
        IMetadataObject metadata,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var match = VideosRegex.Match(uri.AbsoluteUri);
        var channelName = match.Groups["channel"].Value;
        string? cursor = null;

        while (true)
        {
            var json = await GetGraphQlDataAsync(GraphQlRequest.FilterableVideoTower_Videos(channelName, cursor), cancellationToken);
            var videos = json.SelectTokens("$..videos.edges[*].node");

            foreach (var video in videos)
            {
                foreach (var result in ExtractVideo(video, metadata))
                {
                    yield return result;
                }
            }

            var hasNextPage = json.SelectToken("$..pageInfo.hasNextPage")?.Value<bool>() ?? false;
            if (!hasNextPage)
            {
                break;
            }

            cursor = json.SelectToken("$..videos.edges[-1:].cursor")?.ToString();
        }
    }

    private async IAsyncEnumerable<ExtractResult> ExtractVideoAsync(
        Uri uri,
        IMetadataObject metadata,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var match = Consts.VideoRegex.Match(uri.AbsoluteUri);
        var videoId = match.Groups["video"].Value;

        var channelVideoCore = await GetGraphQlDataAsync(GraphQlRequest.ChannelVideoCore(videoId), cancellationToken);
        var channelName = channelVideoCore.SelectToken("$..owner.login")!.ToString();

        var videoMetadata = await GetGraphQlDataAsync(GraphQlRequest.VideoMetadata(channelName, videoId), cancellationToken);
        var video = videoMetadata.SelectToken("$..video");

        foreach (var result in ExtractVideo(video!, metadata))
        {
            yield return result;
        }
    }

    private static IEnumerable<ExtractResult> ExtractVideo(JToken video, IMetadataObject metadata)
    {
        metadata = metadata.Copy();

        var videoId = video.SelectToken("$.id")!.ToString();
        var videoLengthSeconds = video.SelectToken("$.lengthSeconds")?.Value<int>();

        var channelId = video.SelectToken("$..owner.id")?.ToString();
        var channelName = video.SelectToken("$..owner.displayName")?.ToString();
        var channelLogin = video.SelectToken("$..owner.login")?.ToString();

        var thumbnailUrl = video.SelectToken("$..previewThumbnailURL")?.ToString();

        DateTime? publishedAt = null;
        if (DateTime.TryParse(video.SelectToken("$..publishedAt")?.ToString(), out var publishedAtResult))
        {
            publishedAt = publishedAtResult;
            metadata[MetadataVideoPublishedAtKeys] = publishedAt.Value.ToString("yyyyMMdd");
        }

        var title = video.SelectToken("$..title")?.ToString();
        var viewCount = video.SelectToken("$..viewCount")?.Value<int>();
        var contentTags = video.SelectTokens("$..contentTags[*]")
            .Select(t => t["localizedName"]?.ToString())
            .Where(t => t != null);

        metadata[MetadataVideoIdKeys] = videoId;

        if (videoLengthSeconds.HasValue)
        {
            metadata[MetadataVideoLengthSecondsKeys] = videoLengthSeconds.Value;
            metadata[MetadataVideoLengthKeys] = TimeSpanToString(TimeSpan.FromSeconds(videoLengthSeconds.Value));
        }

        metadata[MetadataChannelIdKeys] = channelId;
        metadata[MetadataChannelNameKeys] = channelName;
        metadata[MetadataChannelLoginKeys] = channelLogin;

        metadata[MetadataVideoTitleKeys] = title;
        metadata[MetadataVideoViewCountKeys] = viewCount;

        if (contentTags?.Any() ?? false)
        {
            metadata[MetadataVideoTagsKeys] = string.Join(", ", contentTags);
        }

        var gameMetadata = video.SelectToken("$..game");
        if (gameMetadata != null)
        {
            GetMetadata<string>(metadata, gameMetadata["id"], MetadataGameIdKeys);
            GetMetadata<string>(metadata, gameMetadata["displayName"], MetadataGameNameKeys);
            GetMetadata<string>(metadata, gameMetadata["boxArtURL"], MetadataGameBoxartUrlKeys);
        }

        if (thumbnailUrl != null)
        {
            var thumbnailMetadata = metadata.Copy();

            if (publishedAt.HasValue)
            {
                thumbnailMetadata[MetadataObjectConsts.Origin.ItemIdSeqKeys] = $"{channelId}#video#{publishedAt.Value:yyyyMMdd}_{videoId}#thumb";
            }

            thumbnailMetadata[MetadataObjectConsts.File.ExtensionKeys] = GetExtensionFromUri(new Uri(thumbnailUrl));

            yield return new ExtractResult(
                thumbnailUrl,
                $"{videoId}#thumb",
                JobTaskType.Download,
                metadata: thumbnailMetadata);
        }

        if (publishedAt.HasValue)
        {
            metadata[MetadataObjectConsts.Origin.ItemIdSeqKeys] = $"{channelId}#video#{publishedAt.Value:yyyyMMdd}_{videoId}";
        }

        metadata[MetadataObjectConsts.File.ExtensionKeys] = YoutubeDlFileExtensionTemplate;

        yield return new ExtractResult(
            $"https://www.twitch.tv/videos/{videoId}",
            videoId,
            JobTaskType.Download,
            metadata: metadata);
    }

    private async IAsyncEnumerable<ExtractResult> ExtractClipsAsync(
        Uri uri,
        IMetadataObject metadata,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var match = ClipsRegex.Match(uri.AbsoluteUri);
        var channelName = match.Groups["channel"].Value;
        string? cursor = null;

        while (true)
        {
            var json = await GetGraphQlDataAsync(GraphQlRequest.ClipsCards__User(channelName, cursor), cancellationToken);
            var clips = json.SelectTokens("$..clips.edges[*].node");

            foreach (var clip in clips)
            {
                var clipSlug = clip.SelectToken("$.slug")!.ToString();
                yield return new ExtractResult(
                    $"https://clips.twitch.tv/{clipSlug}",
                    $"clip#{clipSlug}",
                    JobTaskType.Extract,
                    metadata: metadata);
            }

            cursor = json.SelectToken("$..clips.edges[-1:].cursor")?.ToString();
            if (string.IsNullOrEmpty(cursor))
            {
                break;
            }
        }
    }

    private async IAsyncEnumerable<ExtractResult> ExtractClipAsync(
        Uri uri,
        IMetadataObject metadata,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        metadata = metadata.Copy();

        var absoluteUri = uri.AbsoluteUri;
        var match = ClipRegex.Match(absoluteUri);
        if (!match.Success)
        {
            match = ClipAltRegex.Match(absoluteUri);
        }

        var clipSlug = match.Groups["clip"].Value;

        metadata[MetadataClipSlugKeys] = clipSlug;

        var responses = await GetClipDataAsync(clipSlug, cancellationToken);

        var clipsSocialShare = responses.Single(r => r.Operation == "ClipsSocialShare").Data;
        GetMetadata<string>(metadata, clipsSocialShare.SelectToken("$..clip.id"), MetadataClipIdKeys);
        GetMetadata<string>(metadata, clipsSocialShare.SelectToken("$.clip.url"), MetadataClipUrlKeys);
        GetMetadata<string>(metadata, clipsSocialShare.SelectToken("$.clip.title"), MetadataClipTitleKeys);
        GetMetadata<string>(metadata, clipsSocialShare.SelectToken("$..game.id"), MetadataClipGameIdKeys);
        GetMetadata<string>(metadata, clipsSocialShare.SelectToken("$..game.name"), MetadataClipGameNameKeys);

        var comscoreStreamingQuery = responses.Single(r => r.Operation == "ComscoreStreamingQuery").Data;

        DateTime? createdAt = null;
        if (DateTime.TryParse(comscoreStreamingQuery.SelectToken("$..clip.createdAt")?.ToString(), out var createdAtResult))
        {
            createdAt = createdAtResult;
            metadata[MetadataClipCreatedAtKeys] = createdAt.Value.ToString("yyyyMMdd");
        }
        var duration = comscoreStreamingQuery.SelectToken("$..clip.durationSeconds")?.Value<int>();

        metadata[MetadataClipDurationKeys] = TimeSpanToString(duration.HasValue ? TimeSpan.FromSeconds(duration.Value) : null);
        metadata[MetadataClipDurationSecondsKeys] = duration;

        var clipsBroadcasterInfo = responses.Single(r => r.Operation == "ClipsBroadcasterInfo").Data;
        var channelId = GetMetadata<string>(metadata, clipsBroadcasterInfo.SelectToken("$..broadcaster.id"), MetadataChannelIdKeys);
        GetMetadata<string>(metadata, clipsBroadcasterInfo.SelectToken("$..broadcaster.login"), MetadataChannelNameKeys);
        GetMetadata<string>(metadata, clipsBroadcasterInfo.SelectToken("$..broadcaster.displayName"), MetadataChannelLoginKeys);

        var clipsViewCount = responses.Single(r => r.Operation == "ClipsViewCount").Data;
        GetMetadata<int>(metadata, clipsViewCount.SelectToken("$..clip.viewCount"), MetadataClipViewCountKeys);

        var clipsCurator = responses.Single(r => r.Operation == "ClipsCurator").Data;
        GetMetadata<string>(metadata, clipsCurator.SelectToken("$..curator.id"), MetadataClipCuratorIdKeys);
        GetMetadata<string>(metadata, clipsCurator.SelectToken("$..curator.login"), MetadataClipCuratorNameKeys);
        GetMetadata<string>(metadata, clipsCurator.SelectToken("$..curator.displayName"), MetadataClipCuratorLoginKeys);

        var videoAccessTokenClip = responses.Single(r => r.Operation == "VideoAccessToken_Clip").Data;
        var videoUrl = videoAccessTokenClip.SelectToken("$..videoQualities[0].sourceURL")!.ToString();
        var signature = videoAccessTokenClip.SelectToken("$..playbackAccessToken.signature")!.ToString();
        var token = videoAccessTokenClip.SelectToken("$..playbackAccessToken.value")!.ToString();

        if (createdAt.HasValue)
        {
            metadata[MetadataObjectConsts.Origin.ItemIdSeqKeys] = $"{channelId}#clip#{createdAt.Value:yyyyMMdd}_{clipSlug}";
        }

        metadata[MetadataObjectConsts.Origin.UriKeys] = $"https://clips.twitch.tv/{clipSlug}";
        metadata[MetadataObjectConsts.File.ExtensionKeys] = "mp4";

        yield return new ExtractResult(
            $"{videoUrl}?sig={signature}&token={HttpUtility.UrlEncode(token)}",
            clipSlug,
            JobTaskType.Download,
            metadata: metadata);
    }

    private async IAsyncEnumerable<ExtractResult> ExtractAboutAsync(
        Uri uri,
        IMetadataObject metadata,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var match = AboutRegex.Match(uri.AbsoluteUri);
        var channelName = match.Groups["channel"].Value;

        var json = await GetGraphQlDataAsync(GraphQlRequest.ChannelShell(channelName), cancellationToken);
        var channelId = json.SelectToken("$..userOrError.id")?.ToString();

        if (channelId != null)
        {
            await foreach (var result in ExtractEmotesAsync(channelId, metadata, cancellationToken))
            {
                yield return result;
            }
        }
    }

    private async IAsyncEnumerable<ExtractResult> ExtractEmotesAsync(
        string channelId,
        IMetadataObject metadata,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var json = await GetGraphQlDataAsync(GraphQlRequest.EmotePicker_EmotePicker_UserSubscriptionProducts(channelId), cancellationToken);
        var subscriptionProducts = json.SelectToken("$..subscriptionProducts");
        if (subscriptionProducts == null)
        {
            yield break;
        }

        foreach (var subscriptionProduct in subscriptionProducts)
        {
            foreach (var result in ExtractSubscriptionProduct(subscriptionProduct, metadata, channelId))
            {
                yield return result;
            }
        }
    }

    private static IEnumerable<ExtractResult> ExtractSubscriptionProduct(JToken subscriptionProduct, IMetadataObject metadata, string? channelId)
    {
        metadata = metadata.Copy();

        var price = subscriptionProduct.SelectToken("$.price")?.ToString();
        var tier = subscriptionProduct.SelectToken("$.tier")?.ToString();

        metadata[MetadataEmotePriceKeys] = price;
        metadata[MetadataEmoteTierKeys] = tier;

        var emotes = subscriptionProduct.SelectToken("$.emotes");
        if (emotes== null)
        {
            yield break;
        }

        foreach (var emote in emotes)
        {
            var emoteMetadata = metadata.Copy();

            var emoteId = GetMetadata<string>(emoteMetadata, emote.SelectToken("$.id"), MetadataEmoteIdKeys);
            GetMetadata<string>(emoteMetadata, emote.SelectToken("$.setID"), MetadataEmoteSetIdKeys);
            GetMetadata<string>(emoteMetadata, emote.SelectToken("$.token"), MetadataEmoteTokenKeys);
            var assetType = GetMetadata<string>(emoteMetadata, emote.SelectToken("$.assetType"), MetadataEmoteAssetTypeKeys);

            emoteMetadata[MetadataObjectConsts.File.ExtensionKeys] = assetType == "ANIMATED"
                ? "gif"
                : "png";

            if (channelId != null)
            {
                emoteMetadata[MetadataObjectConsts.Origin.ItemIdSeqKeys] = $"{channelId}#emote#{emoteId}";
            }

            yield return new ExtractResult(
                $"https://static-cdn.jtvnw.net/emoticons/v2/{emoteId}/default/dark/1.0",
                emoteId,
                JobTaskType.Download,
                metadata: emoteMetadata);
        }
    }

    private async Task<List<GraphQlResponse>> GetClipDataAsync(
        string clipSlug,
        CancellationToken cancellationToken)
    {
        return await GetGraphQlResponseAsync(
            new[]
            {
                GraphQlRequest.ClipsSocialShare(clipSlug),
                GraphQlRequest.ComscoreStreamingQueryClip(clipSlug),
                GraphQlRequest.ClipsBroadcasterInfo(clipSlug),
                GraphQlRequest.ClipsViewCount(clipSlug),
                GraphQlRequest.ClipsCurator(clipSlug),
                GraphQlRequest.VideoAccessToken_Clip(clipSlug),
            },
            cancellationToken);
    }

    private async Task<JToken> GetGraphQlDataAsync(GraphQlRequest request, CancellationToken cancellationToken)
    {
        var responses = await GetGraphQlResponseAsync(new[] { request }, cancellationToken);
        return responses[0].Data;
    }

    private async Task<List<GraphQlResponse>> GetGraphQlResponseAsync(IEnumerable<GraphQlRequest> requests, CancellationToken cancellationToken)
    {
        using var response = await PostGraphQlAsync(
            requests.Select(r => r.GetRequest()).ToArray(),
            cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JArray.Parse(content);
        return result.Select(r => new GraphQlResponse(r["extensions"]!["operationName"]!.ToString(), r["data"]!)).ToList();
    }

    private async Task<HttpResponseMessage> PostGraphQlAsync(object[] data, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Consts.GraphQlUri)
        {
            Content = JsonContent.Create(data)
        };
        if (!request.Headers.Contains(ClientIdHeader))
        {
            request.Headers.Add(ClientIdHeader, Consts.ClientId);
        }
        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return response;
    }

    private static ExtractType? GetExtractType(Uri uri)
    {
        var absoluteUri = uri.AbsoluteUri;
        if (Consts.VideoRegex.IsMatch(absoluteUri))
        {
            return ExtractType.Video;
        }
        if (ClipRegex.IsMatch(absoluteUri) || ClipAltRegex.IsMatch(absoluteUri))
        {
            return ExtractType.Clip;
        }
        if (ClipsRegex.IsMatch(absoluteUri))
        {
            return ExtractType.Clips;
        }
        if (AboutRegex.IsMatch(absoluteUri))
        {
            return ExtractType.About;
        }
        if (VideosRegex.IsMatch(absoluteUri))
        {
            return ExtractType.Videos;
        }
        if (ChannelUriRegex.IsMatch(absoluteUri))
        {
            return ExtractType.Channel;
        }
        return null;
    }

    private static T? GetMetadata<T>(IMetadataObject metadata, JToken? token, params string[] keys)
    {
        if (token == null)
        {
            return default;
        }

        var value = token.Value<T>();
        metadata[keys] = value;
        return value;
    }

    private static string? GetExtensionFromUri(Uri uri)
    {
        var extension = Path.GetExtension(uri.AbsolutePath);
        return extension.Length > 0 && extension[0] == '.'
            ? extension[1..]
            : extension;
    }

    private static string? TimeSpanToString(TimeSpan? timeSpan)
    {
        return timeSpan.HasValue
            ? timeSpan.Value.Hours > 0
                ? timeSpan.Value.ToString(@"hh\:mm\:ss")
                : timeSpan.Value.ToString(@"mm\:ss")
            : null;
    }

    private enum ExtractType
    {
        Channel,
        Videos,
        Video,
        Clips,
        Clip,
        About,
    }
}
