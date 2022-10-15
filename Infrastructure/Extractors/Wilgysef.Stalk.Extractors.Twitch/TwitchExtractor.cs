using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Dynamic;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using Wilgysef.Stalk.Core.Shared.CacheObjects;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Extractors;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;

namespace Wilgysef.Stalk.Extractors.Twitch;

public class TwitchExtractor : IExtractor
{
    public string Name => "Twitch";

    public ILogger? Logger { get; set; }

    public ICacheObject<string, object?>? Cache { get; set; }

    public IDictionary<string, object?> Config { get; set; } = new Dictionary<string, object?>();

    private const string ChannelRegex = @"(?<channel>[A-Za-z0-9_-]+)";
    private static readonly Regex VideosRegex = new(Consts.UriPrefixRegex + $@"/{ChannelRegex}(?:/(?:videos)?|$)", RegexOptions.Compiled);
    private static readonly Regex ClipsRegex = new(Consts.UriPrefixRegex + $@"/{ChannelRegex}/clips", RegexOptions.Compiled);
    private static readonly Regex ClipRegex = new(Consts.UriPrefixRegex + $@"/{ChannelRegex}/clip/(?<clip>[A-Za-z0-9_-]+)", RegexOptions.Compiled);
    private static readonly Regex ClipAltRegex = new(@"(?:https?://)?clips\.twitch\.tv/(?<clip>[A-Za-z0-9_-]+)", RegexOptions.Compiled);
    private static readonly Regex AboutRegex = new(Consts.UriPrefixRegex + $@"/{ChannelRegex}/about/?", RegexOptions.Compiled);

    private static readonly string[] MetadataVideoIdKeys = new[] { "video", "id" };
    private static readonly string[] MetadataVideoLengthSecondsKeys = new[] { "video", "length_seconds" };
    private static readonly string[] MetadataVideoLengthKeys = new[] { "video", "length" };
    private static readonly string[] MetadataChannelIdKeys = new[] { "channel", "id" };
    private static readonly string[] MetadataChannelNameKeys = new[] { "channel", "name" };
    private static readonly string[] MetadataChannelLoginKeys = new[] { "channel", "login" };
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
            _ => throw new NotImplementedException(),
        };
    }

    public string? GetItemId(Uri uri)
    {
        var absoluteUri = uri.AbsoluteUri;

        var match = Consts.VideoRegex.Match(absoluteUri);
        if (match.Success)
        {
            return match.Groups["video"].Value;
        }

        match = ClipRegex.Match(absoluteUri);
        if (match.Success)
        {
            return match.Groups["clip"].Value;
        }

        match = ClipAltRegex.Match(absoluteUri);
        if (match.Success)
        {
            return match.Groups["clip"].Value;
        }

        return null;
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
        // TODO: use ExtractResults instead?

        await foreach (var result in ExtractVideosAsync(uri, metadata, cancellationToken))
        {
            yield return result;
        }

        await foreach (var result in ExtractClipsAsync(uri, metadata, cancellationToken))
        {
            yield return result;
        }

        await foreach (var result in ExtractAboutAsync(uri, metadata, cancellationToken))
        {
            yield return result;
        }
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
            var json = await GetVideosAsync(channelName, cursor, cancellationToken);
            var videos = json.SelectTokens("$..videos.edges[*].node");

            foreach (var video in videos)
            {
                foreach (var result in ExtractVideo(video, metadata.Copy()))
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

        var channelVideoCore = await GetChannelVideoCoreAsync(videoId, cancellationToken);
        var channelName = channelVideoCore.SelectToken("$..owner.login")!.ToString();

        var videoMetadata = await GetVideoMetadataAsync(channelName, videoId, cancellationToken);
        var video = videoMetadata.SelectToken("$..video");

        foreach (var result in ExtractVideo(video!, metadata.Copy()))
        {
            yield return result;
        }
    }

    private IEnumerable<ExtractResult> ExtractVideo(JToken video, IMetadataObject metadata)
    {
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
        }

        var title = video.SelectToken("$..title")?.ToString();
        var viewCount = video.SelectToken("$..viewCount")?.Value<int>();
        var contentTags = video.SelectTokens("$..contentTags[*]")
            .Select(t => t["localizedName"]?.ToString())
            .Where(t => t != null);

        metadata.SetByParts(videoId, MetadataVideoIdKeys);

        if (videoLengthSeconds.HasValue)
        {
            metadata.SetByParts(videoLengthSeconds.Value, MetadataVideoLengthSecondsKeys);
            metadata.SetByParts(TimeSpanToString(TimeSpan.FromSeconds(videoLengthSeconds.Value)), MetadataVideoLengthKeys);
        }

        metadata.SetByParts(channelId, MetadataChannelIdKeys);
        metadata.SetByParts(channelName, MetadataChannelNameKeys);
        metadata.SetByParts(channelLogin, MetadataChannelLoginKeys);

        metadata.SetByParts(title, MetadataVideoTitleKeys);
        metadata.SetByParts(viewCount, MetadataVideoViewCountKeys);

        if (contentTags?.Any() ?? false)
        {
            metadata.SetByParts(string.Join(", ", contentTags), MetadataVideoTagsKeys);
        }

        var gameMetadata = video.SelectToken("$..game");
        if (gameMetadata != null)
        {
            metadata.SetByParts(gameMetadata["id"]?.ToString(), MetadataGameIdKeys);
            metadata.SetByParts(gameMetadata["displayName"]?.ToString(), MetadataGameNameKeys);
            metadata.SetByParts(gameMetadata["boxArtURL"]?.ToString(), MetadataGameBoxartUrlKeys);
        }

        if (thumbnailUrl != null)
        {
            var thumbnailMetadata = metadata.Copy();
            
            if (publishedAt.HasValue)
            {
                thumbnailMetadata.SetByParts($"{channelId}#{publishedAt.Value:yyyyMMdd}_{videoId}#thumb", MetadataObjectConsts.Origin.ItemIdSeqKeys);
            }

            thumbnailMetadata.SetByParts(GetExtensionFromUri(new Uri(thumbnailUrl)), MetadataObjectConsts.File.ExtensionKeys);

            yield return new ExtractResult(
                thumbnailUrl,
                $"{videoId}#thumb",
                JobTaskType.Download,
                metadata: thumbnailMetadata);
        }

        if (publishedAt.HasValue)
        {
            metadata.SetByParts($"{channelId}#{publishedAt.Value:yyyyMMdd}_{videoId}", MetadataObjectConsts.Origin.ItemIdSeqKeys);
        }

        metadata.SetByParts(YoutubeDlFileExtensionTemplate, MetadataObjectConsts.File.ExtensionKeys);

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
            var json = await GetClipsCardsAsync(channelName, cursor, cancellationToken);
            var clips = json.SelectTokens("$..clips.edges[*].node");

            foreach (var clip in clips)
            {
                var clipSlug = clip.SelectToken("$.slug")!.ToString();
                yield return new ExtractResult(
                    $"https://clips.twitch.tv/{clipSlug}",
                    clipSlug,
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
        var absoluteUri = uri.AbsoluteUri;
        var match = ClipRegex.Match(absoluteUri);
        if (!match.Success)
        {
            match = ClipAltRegex.Match(absoluteUri);
        }

        var clipSlug = match.Groups["clip"].Value;

        metadata.SetByParts(clipSlug, MetadataClipSlugKeys);

        var responses = await GetClipDataAsync(clipSlug, cancellationToken);

        var clipsSocialShare = responses.Single(r => r.Operation == "ClipsSocialShare").Data;

        var clipId = clipsSocialShare.SelectToken("$..clip.id")?.ToString();
        var clipUrl = clipsSocialShare.SelectToken("$.clip.url")!.ToString();
        var title = clipsSocialShare.SelectToken("$.clip.title")?.ToString();

        metadata.SetByParts(clipId, MetadataClipIdKeys);
        metadata.SetByParts(clipUrl, MetadataClipUrlKeys);
        metadata.SetByParts(title, MetadataClipTitleKeys);

        var gameId = clipsSocialShare.SelectToken("$..game.id")?.ToString();
        var gameName = clipsSocialShare.SelectToken("$..game.name")?.ToString();
        
        metadata.SetByParts(gameId, MetadataClipGameIdKeys);
        metadata.SetByParts(gameName, MetadataClipGameNameKeys);

        var comscoreStreamingQuery = responses.Single(r => r.Operation == "ComscoreStreamingQuery").Data;

        DateTime? createdAt = null;
        if (DateTime.TryParse(comscoreStreamingQuery.SelectToken("$..clip.createdAt")?.ToString(), out var createdAtResult))
        {
            createdAt = createdAtResult;
        }
        var duration = comscoreStreamingQuery.SelectToken("$..clip.durationSeconds")?.Value<int>();

        metadata.SetByParts(createdAt?.ToString("yyyyMMdd"), MetadataClipCreatedAtKeys);
        metadata.SetByParts(TimeSpanToString(duration.HasValue ? TimeSpan.FromSeconds(duration.Value) : null), MetadataClipDurationKeys);
        metadata.SetByParts(duration, MetadataClipDurationSecondsKeys);

        var clipsBroadcasterInfo = responses.Single(r => r.Operation == "ClipsBroadcasterInfo").Data;

        var channelId = clipsBroadcasterInfo.SelectToken("$..broadcaster.id")?.ToString();
        var channelLogin = clipsBroadcasterInfo.SelectToken("$..broadcaster.login")?.ToString();
        var channelName = clipsBroadcasterInfo.SelectToken("$..broadcaster.displayName")?.ToString();

        metadata.SetByParts(channelId, MetadataChannelIdKeys);
        metadata.SetByParts(channelName, MetadataChannelNameKeys);
        metadata.SetByParts(channelLogin, MetadataChannelLoginKeys);

        var clipsViewCount = responses.Single(r => r.Operation == "ClipsViewCount").Data;

        var viewCount = clipsViewCount.SelectToken("$..clip.viewCount")?.Value<int>();

        metadata.SetByParts(viewCount, MetadataClipViewCountKeys);

        var clipsCurator = responses.Single(r => r.Operation == "ClipsCurator").Data;

        var curatorId = clipsCurator.SelectToken("$..curator.id")?.ToString();
        var curatorLogin = clipsCurator.SelectToken("$..curator.login")?.ToString();
        var curatorName = clipsCurator.SelectToken("$..curator.displayName")?.ToString();

        metadata.SetByParts(curatorId, MetadataClipCuratorIdKeys);
        metadata.SetByParts(curatorName, MetadataClipCuratorNameKeys);
        metadata.SetByParts(curatorLogin, MetadataClipCuratorLoginKeys);

        var videoAccessTokenClip = responses.Single(r => r.Operation == "VideoAccessToken_Clip").Data;

        var videoUrl = videoAccessTokenClip.SelectToken("$..videoQualities[0].sourceURL")!.ToString();
        var signature = videoAccessTokenClip.SelectToken("$..playbackAccessToken.signature")!.ToString();
        var token = videoAccessTokenClip.SelectToken("$..playbackAccessToken.value")!.ToString();

        if (createdAt.HasValue)
        {
            metadata.SetByParts($"{channelId}#{createdAt.Value:yyyyMMdd}_{clipSlug}", MetadataObjectConsts.Origin.ItemIdSeqKeys);
        }

        metadata.SetByParts($"https://clips.twitch.tv/{clipSlug}", MetadataObjectConsts.Origin.UriKeys);
        metadata.SetByParts("mp4", MetadataObjectConsts.File.ExtensionKeys);

        yield return new ExtractResult(
            $"{videoUrl}?sig={signature}&token={HttpUtility.UrlEncode(token)}",
            clipSlug,
            JobTaskType.Download,
            metadata: metadata);
    }

    private async IAsyncEnumerable<ExtractResult> ExtractAboutAsync(
        Uri uri,
        IMetadataObject metadata,
        CancellationToken cancellationToken)
    {
        var match = AboutRegex.Match(uri.AbsoluteUri);
        var channelName = match.Groups["channel"].Value;

        var json = await GetChannelShellAsync(channelName, cancellationToken);
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
        var json = await GetEmotePickerUserSubscriptionProductsAsync(channelId, cancellationToken);
        var subscriptionProducts = json.SelectTokens("$..subscriptionProducts[*]");

        foreach (var subscriptionProduct in subscriptionProducts)
        {
            foreach (var result in ExtractSubscriptionProduct(subscriptionProduct, metadata.Copy()))
            {
                yield return result;
            }
        }
    }

    private IEnumerable<ExtractResult> ExtractSubscriptionProduct(JToken subscriptionProduct, IMetadataObject metadata)
    {
        var price = subscriptionProduct.SelectToken("$.price")?.ToString();
        var tier = subscriptionProduct.SelectToken("$.tier")?.ToString();

        metadata.SetByParts(price, MetadataEmotePriceKeys);
        metadata.SetByParts(tier, MetadataEmoteTierKeys);

        var emotes = subscriptionProduct.SelectTokens("$.emotes[*]");
        foreach (var emote in emotes)
        {
            var emoteMetadata = metadata.Copy();

            var emoteId = emote.SelectToken("$.id")!.ToString();
            var setId = emote.SelectToken("$.setID")?.ToString();
            var token = emote.SelectToken("$.token")?.ToString();
            var assetType = emote.SelectToken("$.assetType")?.ToString();

            emoteMetadata.SetByParts(emoteId, MetadataEmoteIdKeys);
            emoteMetadata.SetByParts(setId, MetadataEmoteSetIdKeys);
            emoteMetadata.SetByParts(token, MetadataEmoteTokenKeys);
            emoteMetadata.SetByParts(assetType, MetadataEmoteAssetTypeKeys);

            if (assetType == "ANIMATED")
            {
                emoteMetadata.SetByParts("gif", MetadataObjectConsts.File.ExtensionKeys);
            }
            else
            {
                emoteMetadata.SetByParts("png", MetadataObjectConsts.File.ExtensionKeys);
            }

            yield return new ExtractResult(
                $"https://static-cdn.jtvnw.net/emoticons/v2/{emoteId}/default/dark/1.0",
                emoteId,
                JobTaskType.Download,
                metadata: emoteMetadata);
        }
    }

    private async Task<JToken> GetVideosAsync(
        string channelName,
        string? cursor,
        CancellationToken cancellationToken)
    {
        dynamic variables = new ExpandoObject();
        variables.broadcastType = (object?)null;
        variables.channelOwnerLogin = channelName;
        variables.limit = 30;
        variables.videoSort = "TIME";

        if (cursor != null)
        {
            variables.cursor = cursor;
        }

        return await GetGraphQlDataAsync(
            "FilterableVideoTower_Videos",
            "a937f1d22e269e39a03b509f65a7490f9fc247d7f83d6ac1421523e3b68042cb",
            variables,
            cancellationToken: cancellationToken);
    }

    private async Task<JToken> GetChannelVideoCoreAsync(
        string videoId,
        CancellationToken cancellationToken)
    {
        var variables = new
        {
            videoID = videoId,
        };

        return await GetGraphQlDataAsync(
            "ChannelVideoCore",
            "cf1ccf6f5b94c94d662efec5223dfb260c9f8bf053239a76125a58118769e8e2",
            variables,
            cancellationToken: cancellationToken);
    }

    private async Task<JToken> GetVideoMetadataAsync(
        string channelName,
        string videoId,
        CancellationToken cancellationToken)
    {
        var variables = new
        {
            channelLogin = channelName,
            videoID = videoId,
        };

        return await GetGraphQlDataAsync(
            "VideoMetadata",
            "49b5b8f268cdeb259d75b58dcb0c1a748e3b575003448a2333dc5cdafd49adad",
            variables,
            cancellationToken: cancellationToken);
    }

    private async Task<JToken> GetClipsCardsAsync(
        string channelName,
        string? cursor,
        CancellationToken cancellationToken)
    {
        dynamic variables = new ExpandoObject();
        variables.criteria = new
        {
            filter = "ALL_TIME"
        };
        variables.limit = 20;
        variables.login = channelName;

        if (cursor != null)
        {
            variables.cursor = cursor;
        }

        return await GetGraphQlDataAsync(
            "ClipsCards__User",
            "b73ad2bfaecfd30a9e6c28fada15bd97032c83ec77a0440766a56fe0bd632777",
            variables,
            cancellationToken: cancellationToken);
    }

    private async Task<JToken> GetChannelShellAsync(string channelName, CancellationToken cancellationToken)
    {
        var variables = new
        {
            login = channelName
        };

        return await GetGraphQlDataAsync(
            "ChannelShell",
            "580ab410bcd0c1ad194224957ae2241e5d252b2c5173d8e0cce9d32d5bb14efe",
            variables,
            cancellationToken: cancellationToken);
    }

    private async Task<JToken> GetEmotePickerUserSubscriptionProductsAsync(string channelId, CancellationToken cancellationToken)
    {
        var variables = new
        {
            channelOwnerID = channelId,
        };

        return await GetGraphQlDataAsync(
            "EmotePicker_EmotePicker_UserSubscriptionProducts",
            "71b5f829a4576d53b714c01d3176f192cbd0b14973eb1c3d0ee23d5d1b78fd7e",
            variables,
            cancellationToken: cancellationToken);
    }

    private async Task<List<GraphQlResponse>> GetClipDataAsync(
        string slug,
        CancellationToken cancellationToken)
    {
        var slugData = new
        {
            slug
        };

        return await GetGraphQlDataAsync(
            new[]
            {
                new GraphQlRequest(
                    "ClipsSocialShare",
                    slugData,
                    "86533e14855999f00b4c700c3a73149f1ddb5a5948453c77defcb8350e8d108d"),
                new GraphQlRequest(
                    "ComscoreStreamingQuery",
                    new
                    {
                        channel = "",
                        clipSlug = slug,
                        isClip = true,
                        isLive = false,
                        isVodOrCollection = false,
                        vodID = ""
                    },
                    "e1edae8122517d013405f237ffcc124515dc6ded82480a88daef69c83b53ac01"),
                new GraphQlRequest(
                    "ClipsBroadcasterInfo",
                    slugData,
                    "ce258d9536360736605b42db697b3636e750fdb14ff0a7da8c7225bdc2c07e8a"),
                new GraphQlRequest(
                    "ClipsViewCount",
                    slugData,
                    "00209f168e946123d3b911544a57be26391306685e6cae80edf75cdcf55bd979"),
                new GraphQlRequest(
                    "ClipsCurator",
                    slugData,
                    "769e99d9ac3f68e53c63dd902807cc9fbea63dace36c81643d776bcb120902e2"),
                new GraphQlRequest(
                    "VideoAccessToken_Clip",
                    slugData,
                    "36b89d2507fce29e5ca551df756d27c1cfe079e2609642b4390aa4c35796eb11")
            },
            cancellationToken);
    }

    private async Task<JToken> GetGraphQlDataAsync(
        string operation,
        string sha256Hash,
        object variables,
        int version = 1,
        CancellationToken cancellationToken = default)
    {
        var response = await PostGraphQlAsync(
            operation,
            sha256Hash,
            variables,
            version,
            cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JArray.Parse(content);
        return result.Single(t => t["extensions"]?["operationName"]?.ToString() == operation);
    }

    private async Task<List<GraphQlResponse>> GetGraphQlDataAsync(
        IEnumerable<GraphQlRequest> requests,
        CancellationToken cancellationToken = default)
    {
        var response = await PostGraphQlAsync(
            requests.Select(r => r.GetRequest()).ToArray(),
            cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JArray.Parse(content);
        return result.Select(r => new GraphQlResponse(r["extensions"]!["operationName"]!.ToString(), r["data"]!)).ToList();
    }

    private async Task<HttpResponseMessage> PostGraphQlAsync(
        string operation,
        string sha256Hash,
        object variables,
        int version = 1,
        CancellationToken cancellationToken = default)
    {
        var data = new object[] {
            new
            {
                extensions = new
                {
                    persistedQuery = new
                    {
                        sha256Hash,
                        version
                    }
                },
                operationName = operation,
                variables
            }
        };
        return await PostGraphQlAsync(data, cancellationToken);
    }

    private async Task<HttpResponseMessage> PostGraphQlAsync(
        object[] data,
        CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Consts.GraphQlUri)
        {
            Content = JsonContent.Create(data)
        };
        if (!request.Headers.Contains("Client-Id"))
        {
            request.Headers.Add("Client-Id", Consts.ClientId);
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
        return null;
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

    private class GraphQlRequest
    {
        public string Operation { get; }

        public object Variables { get; }

        public string Sha256Hash { get; }

        public int Version { get; }

        public GraphQlRequest(
            string operation,
            object variables,
            string sha256Hash,
            int version = 1)
        {
            Operation = operation;
            Variables = variables;
            Sha256Hash = sha256Hash;
            Version = version;
        }

        public object GetRequest()
        {
            return new
            {
                extensions = new
                {
                    persistedQuery = new
                    {
                        sha256Hash = Sha256Hash,
                        version = Version
                    }
                },
                operationName = Operation,
                variables = Variables
            };
        }
    }

    private class GraphQlResponse
    {
        public string Operation { get; }

        public JToken Data { get; }

        public GraphQlResponse(string operation, JToken data)
        {
            Operation = operation;
            Data = data;
        }
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
