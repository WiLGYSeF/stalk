using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
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

    private const string YouTubeClientVersion = "2.20220929.09.00";

    private const string UriPrefixRegex = @"^(?:https?://)?(?:(?:www\.|m\.)?youtube\.com|youtu\.be)";
    private static readonly Regex ChannelRegex = new(UriPrefixRegex + @"/(?<segment>c(?:hannel)?)/(?<channel>[A-Za-z0-9_-]+)", RegexOptions.Compiled);
    private static readonly Regex VideosRegex = new(UriPrefixRegex + @"/c(?:hannel)?/(?<channel>[A-Za-z0-9_-]+)/videos", RegexOptions.Compiled);
    private static readonly Regex PlaylistRegex = new(UriPrefixRegex + @"/playlist\?", RegexOptions.Compiled);
    private static readonly Regex VideoRegex = new(UriPrefixRegex + @"/watch\?", RegexOptions.Compiled);
    private static readonly Regex CommunityRegex = new(UriPrefixRegex + @"/c(?:hannel)?/(?<channel>[A-Za-z0-9_-]+)/community", RegexOptions.Compiled);

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
            ExtractType.Playlist => ExtractPlaylistAsync(uri, metadata, null, cancellationToken),
            ExtractType.Video => ExtractVideoAsync(uri, metadata, null, cancellationToken),
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
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = GetChannel(uri, out var shortChannel);

        await foreach (var result in ExtractVideosAsync(
            GetVideosUri(channel, shortChannel),
            metadata,
            cancellationToken))
        {
            yield return result;
        }

        await foreach (var result in ExtractCommunityAsync(
            GetCommunityUri(channel, shortChannel),
            metadata,
            cancellationToken))
        {
            yield return result;
        }
    }

    private async IAsyncEnumerable<ExtractResult> ExtractVideosAsync(
        Uri uri,
        IMetadataObject metadata,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = GetChannel(uri, out var shortChannel);
        var longChannel = shortChannel
            ? await GetChannelNameAsync(new Uri(GetChannelUriPrefix(channel, true)), cancellationToken)
            : channel;

        await foreach (var result in ExtractPlaylistAsync(
            GetVideosPlaylistUri(longChannel),
            metadata,
            channel,
            cancellationToken))
        {
            yield return result;
        }
    }

    private async IAsyncEnumerable<ExtractResult> ExtractPlaylistAsync(
        Uri uri,
        IMetadataObject metadata,
        string? channelName = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var json = await GetPlaylistJsonAsync(uri, cancellationToken);
        var playlistId = json.SelectToken("$..playlistVideoListRenderer.playlistId")!.ToString();

        while (true)
        {
            var playlistItems = json.SelectTokens("$..playlistVideoRenderer");
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var playlistItem in playlistItems)
            {
                foreach (var result in ExtractVideo(playlistItem, metadata.Copy()))
                {
                    yield return result;
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
        string? channelName = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        channelName ??= await GetChannelNameAsync(uri, cancellationToken);
        if (string.IsNullOrEmpty(channelName))
        {
            throw new ArgumentException("Could not get channel name from URI.", nameof(uri));
        }

        var videoId = GetVideoId(uri)
            ?? throw new ArgumentException("Could not get video Id from URI.", nameof(uri));

        yield return new ExtractResult(
            uri,
            $"{channelName}#video#{videoId}",
            JobTaskType.Download,
            metadata: metadata);
    }

    private async IAsyncEnumerable<ExtractResult> ExtractCommunityAsync(
        Uri uri,
        IMetadataObject metadata,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
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

            if (isSingle)
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

    private IEnumerable<ExtractResult> ExtractVideo(JToken video, IMetadataObject metadata)
    {
        var videoId = video["videoId"]!.ToString();
        var channelId = video.SelectToken("$.shortBylineText.runs[*].navigationEndpoint.browseEndpoint.browseId");

        yield return new ExtractResult(
            new Uri($"https://www.youtube.com/watch?v={videoId}"),
            $"{channelId}#video#{videoId}",
            JobTaskType.Download,
            metadata: metadata);
    }

    private IEnumerable<ExtractResult> ExtractCommunity(JToken post, IMetadataObject metadata)
    {
        var postId = post["postId"]!.ToString();
        var channelId = post.SelectToken("$..authorEndpoint.browseEndpoint.browseId");

        var contextTextRuns = post["contentText"]?["runs"];

        var publishedTime = post.SelectToken("$.publishedTimeText.runs[*].text");
        if (publishedTime != null)
        {
            metadata["published"] = publishedTime.ToString() + $" from {DateTimeOffset.Now}";
        }

        metadata["votes"] = post["voteCount"]?["simpleText"]?.ToString();

        var commentsCount = post.SelectToken("$..replyButton.buttonRenderer.text.simpleText");
        if (commentsCount != null)
        {
            metadata["comments_count"] = commentsCount.ToString();
        }

        if (contextTextRuns != null)
        {
            var textBuilder = new StringBuilder();
            foreach (var content in contextTextRuns)
            {
                if (content["text"] != null)
                {
                    textBuilder.Append(content["text"]!.ToString());
                }
            }

            yield return new ExtractResult(
                    Encoding.UTF8.GetBytes(textBuilder.ToString()),
                    $"{channelId}#community#{postId}",
                    JobTaskType.Download,
                    metadata: metadata);
        }

        var images = post.SelectToken("$..backstageImageRenderer.image.thumbnails");
        if (images != null)
        {
            var imageUrl = images.LastOrDefault()?["url"]?.ToString();
            if (imageUrl != null)
            {
                imageUrl = GetCommunityImageUrlFromThumbnail(imageUrl);
                yield return new ExtractResult(
                    new Uri(imageUrl),
                    $"{channelId}#community#{postId}-image",
                    JobTaskType.Download,
                    metadata: metadata);
            }
        }
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

    private async Task<JObject> GetYtInitialData(Uri uri, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(uri, cancellationToken);
        response.EnsureSuccessStatusCode();

        var doc = new HtmlDocument();
        doc.Load(response.Content.ReadAsStream(cancellationToken));

        var scripts = doc.DocumentNode.SelectNodes("//script");

        var prefix = "var ytInitialData = ";
        var initialData = scripts.Single(n => n.InnerHtml.TrimStart().StartsWith(prefix));
        var trimmedHtml = initialData.InnerHtml.Trim();
        var json = trimmedHtml.Substring(prefix.Length, trimmedHtml.Length - prefix.Length - 1);

        return JObject.Parse(json)
            ?? throw new ArgumentException("Could not get initial playlist data.", nameof(uri));
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

    private async Task<string> GetChannelNameAsync(Uri uri, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(uri, cancellationToken);
        response.EnsureSuccessStatusCode();

        var doc = new HtmlDocument();
        doc.Load(response.Content.ReadAsStream(cancellationToken));

        var metaChannelId = doc.DocumentNode.SelectSingleNode("//meta[@itemprop=\"channelId\"]");
        return metaChannelId.Attributes["content"].Value;
    }

    private string GetCommunityImageUrlFromThumbnail(string url)
    {
        var imageRegex = new Regex(@"^https://yt3\.ggpht\.com/(?<image>[A-Za-z0-9_-]+)=s(?<size>[0-9]+)", RegexOptions.Compiled);
        var match = imageRegex.Match(url);
        if (!match.Success)
        {
            return url;
        }

        return $"https://yt3.ggpht.com/{match.Groups["image"].Value}=s{match.Groups["size"].Value}-nd-v1";
    }

    private static string? GetVideoId(Uri uri)
    {
        var query = HttpUtility.ParseQueryString(uri.Query);
        return query.Get("v");
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

    private enum ExtractType
    {
        Channel,
        Videos,
        Playlist,
        Video,
        Community,
    }
}
