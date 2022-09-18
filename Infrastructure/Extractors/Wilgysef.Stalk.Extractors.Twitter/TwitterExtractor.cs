using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Dynamic;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Wilgysef.Stalk.Core.Shared.CacheObjects;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Extractors;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;

namespace Wilgysef.Stalk.Extractors.Twitter;

public class TwitterExtractor : IExtractor
{
    // TODO: login?
    // TODO: tweet replies?

    private const string UserByScreenNameEndpoint = "https://twitter.com/i/api/graphql/vG3rchZtwqiwlKgUYCrTRA/UserByScreenName";
    private const string UserTweetsEndpoint = "https://twitter.com/i/api/graphql/q881FFtQa69KN7jS9h_EDA/UserTweets";
    private const string TweetDetailEndpoint = "https://twitter.com/i/api/graphql/Nze3idtpjn4wcl09GpmDRg/TweetDetail";

    private const string GuestTokenEndpoint = "https://api.twitter.com/1.1/guest/activate.json";

    private const string DefaultAuthenticationBearerToken = "AAAAAAAAAAAAAAAAAAAAANRILgAAAAAAnNwIzUejRCOuH5E6I8xnZz4puTs%3D1Zv7ttfk8LF81IUq16cHjhLTvJu4FA33AGWWjCpTnA";

    private const string AuthorizationHeader = "Authorization";

    private const string UserIdCacheKey = "UserId";
    private const string GuestTokenCacheKey = "GuestToken";

    private static readonly Dictionary<string, int> Month3Letters = new()
    {
        { "jan", 1 },
        { "feb", 2 },
        { "mar", 3 },
        { "apr", 4 },
        { "may", 5 },
        { "jun", 6 },
        { "jul", 7 },
        { "aug", 8 },
        { "sep", 9 },
        { "oct", 10 },
        { "nov", 11 },
        { "dec", 12 },
    };

    public string Name => "Twitter";

    public ILogger? Logger { get; set; }

    public ICacheObject<string, object?>? Cache { get; set; }

    public IDictionary<string, object?> Config { get; set; } = new Dictionary<string, object?>();

    private readonly Regex _uriRegex = new(@"^(?:https?://)?(?:(?:www|mobile)\.)?twitter\.com(?:\:(?:80|443))?/(?<user>[^/]+)(?:/status/(?<tweet>[0-9]+))?", RegexOptions.Compiled);
    private readonly Regex _mediaUrlRegex = new(@"^(?:https://)?pbs\.twimg\.com/media/(?<id>[A-Za-z0-9_]+)\.(?<extension>[A-Za-z0-9]+)");

    private HttpClient _httpClient;

    public TwitterExtractor(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public bool CanExtract(Uri uri)
    {
        return _uriRegex.IsMatch(uri.AbsoluteUri);
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
            ExtractType.User => ExtractUserAsync(uri, itemData, metadata, cancellationToken),
            ExtractType.Tweet => ExtractTweetAsync(uri, itemData, metadata, cancellationToken),
            _ => throw new ArgumentException("Cannot extract URI.", nameof(uri)),
        };
    }

    public void SetHttpClient(HttpClient client)
    {
        _httpClient?.Dispose();
        _httpClient = client;
    }

    private async IAsyncEnumerable<ExtractResult> ExtractUserAsync(
        Uri uri,
        string? itemData,
        IMetadataObject metadata,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var match = _uriRegex.Match(uri.AbsoluteUri);
        var userScreenName = match.Groups["user"].Value;

        var userId = await GetUserIdAsync(userScreenName, cancellationToken);
        string? cursor = null;

        while (true)
        {
            var userTweetsUri = GetUserTweetsUri(userId, cursor);
            var response = await GetAsync(userTweetsUri, cancellationToken);

            var data = await response.Content.ReadAsStringAsync(cancellationToken);
            var jObject = JObject.Parse(data);

            var tweets = jObject.SelectTokens(@"$.data.user.result.timeline_v2.timeline.instructions[?(@.type=='TimelineAddEntries')].entries[*].content.itemContent.tweet_results.result");
            if (!tweets.Any())
            {
                break;
            }

            foreach (var tweet in tweets)
            {
                foreach (var result in ExtractTweet(tweet, metadata))
                {
                    yield return result;
                }
            }

            cursor = jObject.SelectTokens(@"$.data.user.result.timeline_v2.timeline.instructions[?(@.type=='TimelineAddEntries')].entries[*].content")
                .FirstOrDefault(t => t["cursorType"]?.ToString() == "Bottom")
                ?["value"]?.ToString()
                ?? "";
            if (cursor.Length == 0)
            {
                break;
            }
        }
    }

    private async IAsyncEnumerable<ExtractResult> ExtractTweetAsync(
        Uri uri,
        string? itemData,
        IMetadataObject metadata,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var match = _uriRegex.Match(uri.AbsoluteUri);
        var tweetId = match.Groups["tweet"].Value;

        var tweetUri = GetTweetUri(tweetId);
        var response = await GetAsync(tweetUri, cancellationToken);

        var data = await response.Content.ReadAsStringAsync(cancellationToken);
        var jObject = JObject.Parse(data);

        var tweet = jObject.SelectToken($"$.data.threaded_conversation_with_injections_v2.instructions[?(@.type=='TimelineAddEntries')].entries..[?(@.rest_id=='{tweetId}')]");

        foreach (var result in ExtractTweet(tweet, metadata))
        {
            yield return result;
        }
    }

    private IEnumerable<ExtractResult> ExtractTweet(JToken tweet, IMetadataObject metadata)
    {
        var userScreenName = GetUserScreenName(tweet);
        var userId = GetUserId(tweet);
        var legacy = tweet["legacy"];
        var tweetId = tweet["rest_id"].ToString();

        metadata["created_at"] = TryParseDateTime(legacy["created_at"]?.ToString(), out _);
        metadata["favorite_count"] = legacy["favorite_count"]?.Value<int>();
        metadata["is_quote_status"] = legacy["is_quote_status"]?.Value<bool>();
        metadata["lang"] = legacy["lang"]?.ToString();
        metadata["possibly_sensitive"] = legacy["possibly_sensitive"]?.Value<bool>();
        metadata["quote_count"] = legacy["quote_count"]?.Value<int>();
        metadata["reply_count"] = legacy["reply_count"]?.Value<int>();
        metadata["retweet_count"] = legacy["retweet_count"]?.Value<int>();
        metadata["tweet_id"] = tweetId;
        metadata.SetByParts(userId, "user", "id");
        metadata.SetByParts(userScreenName, "user", "screen_name");

        foreach (var result in ExtractTweetMedia(tweet, metadata, true))
        {
            yield return result;
        }

        var displayTextRange = legacy["display_text_range"]?.Values<int>().ToList();
        var fullText = legacy["full_text"]?.ToString();

        if (fullText != null)
        {
            if (displayTextRange != null && displayTextRange.Count >= 2)
            {
                fullText = fullText.Substring(displayTextRange[0], displayTextRange[1]);
            }
        }
        else
        {
            fullText = "";
        }

        var entityUrls = GetEntityUrls(tweet);
        if (entityUrls.Count > 0)
        {
            fullText += "\n" + string.Join('\n', entityUrls);
        }

        var textMetadata = metadata.Copy();
        textMetadata.SetByParts("txt", "file", "extension");

        SetRetweetMetadata(tweet, textMetadata);

        if (fullText.Length > 0)
        {
            yield return new ExtractResult(
                new Uri($"data:text/plain;base64,{Convert.ToBase64String(Encoding.UTF8.GetBytes(fullText))}"),
                $"{userId}#{tweetId}",
                JobTaskType.Download,
                metadata: textMetadata);
        }
    }

    private IEnumerable<ExtractResult> ExtractTweetMedia(JToken tweet, IMetadataObject metadata, bool largestSize)
    {
        foreach (var media in GetEntitiesMedia(tweet, metadata, largestSize))
        {
            yield return media;
        }

        foreach (var media in GetExtendedEntitiesMedia(tweet, metadata))
        {
            yield return media;
        }
    }

    private IEnumerable<ExtractResult> GetEntitiesMedia(JToken tweet, IMetadataObject metadata, bool largestSize)
    {
        var userId = GetUserId(tweet);
        var legacy = tweet["legacy"];
        var tweetId = legacy["id_str"]?.ToString();
        var media = legacy["entities"]?["media"];
        if (media == null)
        {
            yield break;
        }

        foreach (var mediaItem in media)
        {
            var mediaId = mediaItem["id_str"]?.ToString();
            var mediaUrl = mediaItem["media_url_https"]?.ToString();
            if (mediaId == null || mediaUrl == null)
            {
                continue;
            }

            var mediaMetadata = metadata.Copy();
            mediaMetadata["media_id"] = mediaId;
            mediaMetadata.SetByParts(GetExtensionFromUri(new Uri(mediaUrl)), "file", "extension");

            if (largestSize)
            {
                mediaUrl = GetLargestSizeMediaUrl(mediaUrl, mediaItem["sizes"]);
            }

            yield return new ExtractResult(
                new Uri(mediaUrl),
                $"{userId}#{tweetId}#{mediaId}",
                JobTaskType.Download,
                metadata: mediaMetadata);
        }
    }

    private IEnumerable<ExtractResult> GetExtendedEntitiesMedia(JToken tweet, IMetadataObject metadata)
    {
        var userId = GetUserId(tweet);
        var legacy = tweet["legacy"];
        var tweetId = legacy["id_str"]?.ToString();
        var media = legacy["extended_entities"]?["media"];
        if (media == null)
        {
            yield break;
        }

        foreach (var mediaItem in media)
        {
            var mediaId = mediaItem["id_str"]?.ToString();
            var videoInfo = mediaItem["video_info"];
            if (mediaId == null || videoInfo == null)
            {
                continue;
            }

            var variants = videoInfo["variants"].Children().ToList();
            var bestVariant = variants.OrderByDescending(v => v["bitrate"]).First();
            var videoUri = new Uri(bestVariant["url"].ToString());

            var mediaMetadata = metadata.Copy();
            mediaMetadata["media_id"] = mediaId;
            mediaMetadata.SetByParts(GetExtensionFromUri(videoUri), "file", "extension");
            mediaMetadata.SetByParts(bestVariant["bitrate"]?.Value<int>(), "video", "bitrate");
            mediaMetadata.SetByParts(videoInfo["duration_millis"]?.Value<int>(), "video", "duration_millis");
            mediaMetadata.SetByParts(mediaItem["mediaStats"]?["viewCount"]?.Value<int>(), "video", "view_count");

            yield return new ExtractResult(
                videoUri,
                $"{userId}#{tweetId}#{mediaId}",
                JobTaskType.Download,
                metadata: mediaMetadata);
        }
    }

    private List<string> GetEntityUrls(JToken tweet)
    {
        var entityUrls = new List<string>();

        var legacy = tweet["legacy"];
        var urls = legacy["entities"]?["urls"];
        if (urls == null)
        {
            return entityUrls;
        }

        foreach (var url in urls)
        {
            if (url["expanded_url"] != null)
            {
                entityUrls.Add(url["expanded_url"]!.ToString());
            }
        }
        return entityUrls;
    }

    private string? GetUserId(JToken tweet)
    {
        return tweet.SelectToken(@"$.core.user_results.result.rest_id")?.ToString();
    }

    private string? GetUserScreenName(JToken tweet)
    {
        return tweet.SelectToken(@"$.core.user_results.result.legacy.screen_name")?.ToString();
    }

    private string GetLargestSizeMediaUrl(string uri, JToken? sizes)
    {
        if (sizes == null || sizes["large"] == null)
        {
            return uri;
        }

        var match = _mediaUrlRegex.Match(uri);
        return match.Success
            ? $"https://pbs.twimg.com/media/{match.Groups["id"]}?format={match.Groups["extension"]}&name=large"
            : uri;
    }

    private bool SetRetweetMetadata(JToken tweet, IMetadataObject metadata)
    {
        var legacy = tweet["legacy"];
        var retweetedTweet = legacy["retweeted_status_result"]?["result"];
        if (retweetedTweet == null)
        {
            return false;
        }

        metadata.SetByParts(retweetedTweet["rest_id"]?.ToString(), "retweet", "tweet_id");
        metadata.SetByParts(GetUserId(retweetedTweet), "retweet", "user", "id");
        metadata.SetByParts(GetUserScreenName(retweetedTweet), "retweet", "user", "screen_name");
        return true;
    }

    private async Task<string> GetUserIdAsync(string userScreenName, CancellationToken cancellationToken)
    {
        if (Cache?.TryGetValue(UserIdCacheKey, out var userIdObj) ?? false)
        {
            return (string)userIdObj!;
        }

        var variables = JsonSerializer.Serialize(new
        {
            screen_name = userScreenName,
            withSafetyModeUserFields = true,
            withSuperFollowsUserFields = true,
        });

        var features = JsonSerializer.Serialize(new
        {
            responsive_web_graphql_timeline_navigation_enabled = false,
        });

        var uri = new Uri($"{UserByScreenNameEndpoint}?variables={variables}&features={features}");
        var response = await GetAsync(uri, cancellationToken);

        var data = await response.Content.ReadAsStringAsync(cancellationToken);
        var jObject = JObject.Parse(data);

        var userId = jObject.SelectToken("$.data.user.result.rest_id")!.ToString();
        Cache?.Set(UserIdCacheKey, userId);
        return userId;
    }

    private async Task<string> GetGuestTokenAsync(CancellationToken cancellationToken)
    {
        if (Cache?.TryGetValue(GuestTokenCacheKey, out var guestTokenObj) ?? false)
        {
            return (string)guestTokenObj!;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, GuestTokenEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", DefaultAuthenticationBearerToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadAsStringAsync(cancellationToken);
        var jObject = JObject.Parse(data);

        var guestToken = jObject["guest_token"]?.ToString();
        if (guestToken == null)
        {
            throw new Exception("Could not get guest token.");
        }

        Cache?.Set(GuestTokenCacheKey, guestToken);
        return guestToken;
    }

    private async Task<HttpResponseMessage> GetAsync(Uri uri, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", DefaultAuthenticationBearerToken);
        request.Headers.Add("x-guest-token", await GetGuestTokenAsync(cancellationToken));
        return await GetAsync(request, cancellationToken);
    }

    private async Task<HttpResponseMessage> GetAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return response;
    }

    private Uri GetUserTweetsUri(string userId, string? cursor)
    {
        dynamic variablesObject = new ExpandoObject();
        variablesObject.userId = userId;
        variablesObject.count = 40;
        variablesObject.includePromotedContent = true;
        variablesObject.withQuickPromoteEligibilityTweetFields = true;
        variablesObject.withSuperFollowsUserFields = true;
        variablesObject.withDownvotePerspective = false;
        variablesObject.withReactionsMetadata = false;
        variablesObject.withReactionsPerspective = false;
        variablesObject.withSuperFollowsTweetFields = true;
        variablesObject.withVoice = true;
        variablesObject.withV2Timeline = true;

        if (cursor != null)
        {
            variablesObject.cursor = cursor;
        }

        var variables = JsonSerializer.Serialize(variablesObject);

        var features = JsonSerializer.Serialize(new
        {
            responsive_web_graphql_timeline_navigation_enabled = false,
            unified_cards_ad_metadata_container_dynamic_card_content_query_enabled = false,
            dont_mention_me_view_api_enabled = true,
            responsive_web_uc_gql_enabled = true,
            vibe_api_enabled = true,
            responsive_web_edit_tweet_api_enabled = true,
            graphql_is_translatable_rweb_tweet_is_translatable_enabled = false,
            standardized_nudges_misinfo = true,
            tweet_with_visibility_results_prefer_gql_limited_actions_policy_enabled = false,
            interactive_text_enabled = true,
            responsive_web_text_conversations_enabled = false,
            responsive_web_enhance_cards_enabled = true
        });

        return new Uri($"{UserTweetsEndpoint}?variables={variables}&features={features}");
    }

    private Uri GetTweetUri(string tweetId)
    {
        var variables = JsonSerializer.Serialize(new
        {
            focalTweetId = tweetId,
            referrer = "profile",
            //rux_context = "",
            with_rux_injections = true,
            includePromotedContent = true,
            withCommunity = true,
            withQuickPromoteEligibilityTweetFields = true,
            withBirdwatchNotes = false,
            withSuperFollowsUserFields = true,
            withDownvotePerspective = false,
            withReactionsMetadata = false,
            withReactionsPerspective = false,
            withSuperFollowsTweetFields = true,
            withVoice = true,
            withV2Timeline = true,
        });

        var features = JsonSerializer.Serialize(new
        {
            responsive_web_graphql_timeline_navigation_enabled = false,
            unified_cards_ad_metadata_container_dynamic_card_content_query_enabled = false,
            dont_mention_me_view_api_enabled = true,
            responsive_web_uc_gql_enabled = true,
            vibe_api_enabled = true,
            responsive_web_edit_tweet_api_enabled = true,
            graphql_is_translatable_rweb_tweet_is_translatable_enabled = false,
            standardized_nudges_misinfo = true,
            tweet_with_visibility_results_prefer_gql_limited_actions_policy_enabled = false,
            interactive_text_enabled = true,
            responsive_web_text_conversations_enabled = false,
            responsive_web_enhance_cards_enabled = true,
        });

        return new Uri($"{TweetDetailEndpoint}?variables={variables}&features={features}");
    }

    private ExtractType? GetExtractType(Uri uri)
    {
        var match = _uriRegex.Match(uri.AbsoluteUri);
        if (match.Groups["tweet"].Success)
        {
            return ExtractType.Tweet;
        }
        if (match.Groups["user"].Success)
        {
            return ExtractType.User;
        }
        return null;
    }

    private string? GetExtensionFromUri(Uri uri)
    {
        var extension = Path.GetExtension(uri.AbsolutePath);
        return extension.Length > 0 && extension[0] == '.'
            ? extension[1..]
            : extension;
    }

    private object TryParseDateTime(string datetime, out bool success)
    {
        var result = ParseDateTime(datetime);
        success = result.HasValue;
        return result.HasValue ? result.Value : datetime;
    }

    private DateTime? ParseDateTime(string datetime)
    {
        try
        {
            var split = datetime.Split(' ');
            var month = split[1];
            var day = split[2];
            var hourMinuteSecond = split[3];
            var year = split[5];

            var timeSplit = hourMinuteSecond.Split(':');
            var hour = timeSplit[0];
            var minute = timeSplit[1];
            var second = timeSplit[2];

            return new DateTime(
                int.Parse(year),
                Month3LetterToInt(month)!.Value,
                int.Parse(day),
                int.Parse(hour),
                int.Parse(minute),
                int.Parse(second));
        }
        catch
        {
            return null;
        }
    }

    private static int? Month3LetterToInt(string month)
    {
        return Month3Letters.TryGetValue(month.ToLower(), out var value) ? value : null;
    }

    private enum ExtractType
    {
        User,
        Tweet,
    }
}
