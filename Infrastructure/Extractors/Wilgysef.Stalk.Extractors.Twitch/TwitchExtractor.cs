using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Dynamic;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
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

    private const string UriPrefixRegex = @"(?:https?://)?(?:www\.)?twitch\.tv";
    private const string UsernameRegex = @"(?<user>[A-Za-z0-9_-]+)";
    private static readonly Regex VideosRegex = new(UriPrefixRegex + $@"/{UsernameRegex}(?:/(?:videos)?|$)", RegexOptions.Compiled);
    private static readonly Regex VideoRegex = new(UriPrefixRegex + @"/videos/(?<video>[0-9]+)", RegexOptions.Compiled);
    private static readonly Regex ClipsRegex = new(UriPrefixRegex + $@"/{UsernameRegex}/clips", RegexOptions.Compiled);
    private static readonly Regex ClipRegex = new(UriPrefixRegex + $@"/{UsernameRegex}/clip/(?<clip>[A-Za-z0-9_-]+)", RegexOptions.Compiled);

    private static readonly string[] MetadataVideoIdKeys = new[] { "video", "id" };
    private static readonly string[] MetadataVideoLengthSecondsKeys = new[] { "video", "length_seconds" };
    private static readonly string[] MetadataVideoLengthKeys = new[] { "video", "length" };
    private static readonly string[] MetadataUserIdKeys = new[] { "user", "id" };
    private static readonly string[] MetadataUserNameKeys = new[] { "user", "name" };
    private static readonly string[] MetadataUserLoginKeys = new[] { "user", "login" };
    private static readonly string[] MetadataVideoTitleKeys = new[] { "video", "title" };
    private static readonly string[] MetadataVideoViewCountKeys = new[] { "video", "view_count" };
    private static readonly string[] MetadataVideoTagsKeys = new[] { "video", "tags" };
    private static readonly string[] MetadataGameIdKeys = new[] { "video", "game", "id" };
    private static readonly string[] MetadataGameNameKeys = new[] { "video", "game", "name" };
    private static readonly string[] MetadataGameBoxartUrlKeys = new[] { "video", "game", "boxart_url" };

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
            ExtractType.Videos => ExtractVideosAsync(uri, metadata, cancellationToken),
            ExtractType.Video => ExtractVideoAsync(uri, metadata, cancellationToken),
            ExtractType.Clips => ExtractClipsAsync(uri, metadata, cancellationToken),
            ExtractType.Clip => ExtractClipAsync(uri, metadata, cancellationToken),
            _ => throw new NotImplementedException(),
        };
    }

    public string? GetItemId(Uri uri)
    {
        var absoluteUri = uri.AbsoluteUri;

        var match = VideoRegex.Match(absoluteUri);
        if (match.Success)
        {
            return match.Groups["video"].Value;
        }

        match = ClipRegex.Match(absoluteUri);
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

    private async IAsyncEnumerable<ExtractResult> ExtractVideosAsync(
        Uri uri,
        IMetadataObject metadata,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var match = VideosRegex.Match(uri.AbsoluteUri);
        var username = match.Groups["user"].Value;
        string? cursor = null;

        while (true)
        {
            var json = await GetVideosAsync(username, cursor, cancellationToken);
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

            cursor = json.SelectToken("$..videos.edges[0].cursor")?.ToString();
        }
    }

    private async IAsyncEnumerable<ExtractResult> ExtractVideoAsync(
        Uri uri,
        IMetadataObject metadata,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var match = VideoRegex.Match(uri.AbsoluteUri);
        var videoId = match.Groups["video"].Value;

        var channelVideoCore = await GetChannelVideoCoreAsync(videoId, cancellationToken);
        var channelName = channelVideoCore.SelectToken("$..owner.login")!.ToString();

        var videoMetadata = await GetVideoMetadataAsync(channelName, videoId, cancellationToken);
        var video = videoMetadata.SelectToken("$..video");

        foreach (var result in ExtractVideo(video, metadata))
        {
            yield return result;
        }
    }

    private IEnumerable<ExtractResult> ExtractVideo(JToken video, IMetadataObject metadata)
    {
        var videoId = video.SelectToken("$.id")!.ToString();
        var videoLengthSeconds = video.SelectToken("$.lengthSeconds")?.Value<int>();

        var userId = video.SelectToken("$..owner.id")?.ToString();
        var userName = video.SelectToken("$..owner.displayName")?.ToString();
        var userLogin = video.SelectToken("$..owner.login")?.ToString();

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

        metadata.SetByParts(userId, MetadataUserIdKeys);
        metadata.SetByParts(userName, MetadataUserNameKeys);
        metadata.SetByParts(userLogin, MetadataUserLoginKeys);

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
                thumbnailMetadata.SetByParts($"{userId}#{publishedAt.Value:yyyyMMdd}_{videoId}#thumb", MetadataObjectConsts.Origin.ItemIdSeqKeys);
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
            metadata.SetByParts($"{userId}#{publishedAt.Value:yyyyMMdd}_{videoId}", MetadataObjectConsts.Origin.ItemIdSeqKeys);
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
        await Task.Delay(0);
        yield return new ExtractResult("", "", JobTaskType.Extract);
    }

    private async IAsyncEnumerable<ExtractResult> ExtractClipAsync(
        Uri uri,
        IMetadataObject metadata,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Delay(0);
        yield return new ExtractResult("", "", JobTaskType.Extract);
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
        if (VideoRegex.IsMatch(absoluteUri))
        {
            return ExtractType.Video;
        }
        if (ClipsRegex.IsMatch(absoluteUri))
        {
            return ExtractType.Clips;
        }
        if (ClipRegex.IsMatch(absoluteUri))
        {
            return ExtractType.Clip;
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

    private enum ExtractType
    {
        Videos,
        Video,
        Clips,
        Clip,
    }
}
