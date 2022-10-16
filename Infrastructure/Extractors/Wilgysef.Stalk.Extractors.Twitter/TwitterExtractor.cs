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

    private const string UserIdCacheKey = "UserIds";
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

    private static readonly Regex UriRegex = new(@"^(?:https?://)?(?:(?:www|mobile)\.)?twitter\.com(?:\:(?:80|443))?/(?<user>[^/]+)(?:/status/(?<tweet>[0-9]+))?", RegexOptions.Compiled);
    private static readonly Regex MediaUrlRegex = new(@"^(?:https://)?pbs\.twimg\.com/media/(?<id>[A-Za-z0-9_]+)\.(?<extension>[A-Za-z0-9]+)");

    private HttpClient _httpClient;

    public TwitterExtractor(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public bool CanExtract(Uri uri)
    {
        return UriRegex.IsMatch(uri.AbsoluteUri);
    }

    public IAsyncEnumerable<ExtractResult> ExtractAsync(
       Uri uri,
       string? itemData,
       IMetadataObject metadata,
       CancellationToken cancellationToken = default)
    {
        return GetExtractType(uri) switch
        {
            ExtractType.User => ExtractUserAsync(uri, metadata, cancellationToken),
            ExtractType.Tweet => ExtractTweetAsync(uri, metadata, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(uri)),
        };
    }

    public string? GetItemId(Uri uri)
    {
        var match = UriRegex.Match(uri.AbsoluteUri);
        if (!match.Success)
        {
            return null;
        }

        var user = match.Groups["user"];
        var tweet = match.Groups["tweet"];
        if (!tweet.Success)
        {
            return null;
        }

        var userId = GetUserIdFromCache(user.Value);
        return userId != null ? $"{userId}#{tweet.Value}" : null;
    }

    public void SetHttpClient(HttpClient client)
    {
        _httpClient = client;
    }

    private async IAsyncEnumerable<ExtractResult> ExtractUserAsync(
        Uri uri,
        IMetadataObject metadata,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var match = UriRegex.Match(uri.AbsoluteUri);
        var userScreenName = match.Groups["user"].Value;

        var userId = await GetUserIdAsync(userScreenName, cancellationToken);
        string? cursor = null;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var userTweetsUri = GetUserTweetsApiUri(userId, cursor);
            var json = await GetJsonAsync(userTweetsUri, cancellationToken);

            var tweets = json.SelectTokens(@"$.data.user.result.timeline_v2.timeline.instructions[?(@.type=='TimelineAddEntries')].entries[*].content.itemContent.tweet_results.result");
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

            cursor = json.SelectTokens(@"$.data.user.result.timeline_v2.timeline.instructions[?(@.type=='TimelineAddEntries')].entries[*].content")
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
        IMetadataObject metadata,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var match = UriRegex.Match(uri.AbsoluteUri);
        var tweetId = match.Groups["tweet"].Value;

        var tweetUri = GetTweetApiUri(tweetId);
        var json = await GetJsonAsync(tweetUri, cancellationToken);

        var tweet = json.SelectToken($"$.data.threaded_conversation_with_injections_v2.instructions[?(@.type=='TimelineAddEntries')].entries..[?(@.rest_id=='{tweetId}')]")!;

        foreach (var result in ExtractTweet(tweet, metadata))
        {
            yield return result;
        }
    }

    private static IEnumerable<ExtractResult> ExtractTweet(JToken tweet, IMetadataObject metadata)
    {
        metadata = metadata.Copy();

        var userScreenName = GetUserScreenName(tweet)!;
        var userId = GetUserId(tweet);
        var legacy = tweet["legacy"]!;
        var tweetId = tweet["rest_id"]!.ToString();

        var createdAtString = legacy["created_at"]?.ToString();
        var createdAt = ParseDateTime(createdAtString ?? "");
        metadata["created_at"] = createdAt.ToString() ?? createdAtString;
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

        var retweetedTweet = legacy["retweeted_status_result"]?["result"];
        if (retweetedTweet != null)
        {
            metadata.SetByParts(retweetedTweet["rest_id"]?.ToString(), "retweet", "tweet_id");
            metadata.SetByParts(GetUserId(retweetedTweet), "retweet", "user", "id");
            metadata.SetByParts(GetUserScreenName(retweetedTweet), "retweet", "user", "screen_name");
        }

        metadata.SetByParts("txt", MetadataObjectConsts.File.ExtensionKeys);
        metadata.SetByParts(GetTweetUri(userScreenName, tweetId), MetadataObjectConsts.Origin.UriKeys);

        if (fullText.Length > 0)
        {
            yield return new ExtractResult(
                Encoding.UTF8.GetBytes(fullText),
                mediaType: "text/plain;charset=UTF-8",
                itemId: $"{userId}#{tweetId}",
                metadata: metadata);
        }
    }

    private static IEnumerable<ExtractResult> ExtractTweetMedia(JToken tweet, IMetadataObject metadata, bool largestSize)
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

    private static IEnumerable<ExtractResult> GetEntitiesMedia(JToken tweet, IMetadataObject metadata, bool largestSize)
    {
        var userId = GetUserId(tweet);
        var legacy = tweet["legacy"]!;
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
            mediaMetadata.SetByParts(GetExtensionFromUri(new Uri(mediaUrl)), MetadataObjectConsts.File.ExtensionKeys);

            if (largestSize)
            {
                mediaUrl = GetLargestSizeMediaUrl(mediaUrl, mediaItem["sizes"]);
            }

            yield return new ExtractResult(
                mediaUrl,
                $"{userId}#{tweetId}#{mediaId}",
                JobTaskType.Download,
                metadata: mediaMetadata);
        }
    }

    private static IEnumerable<ExtractResult> GetExtendedEntitiesMedia(JToken tweet, IMetadataObject metadata)
    {
        var userId = GetUserId(tweet);
        var legacy = tweet["legacy"]!;
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

            var variants = videoInfo["variants"]!.Children().ToList();
            var bestVariant = variants.OrderByDescending(v => v["bitrate"]).First();
            var videoUri = new Uri(bestVariant["url"]!.ToString());

            var mediaMetadata = metadata.Copy();
            mediaMetadata["media_id"] = mediaId;
            mediaMetadata.SetByParts(GetExtensionFromUri(videoUri), MetadataObjectConsts.File.ExtensionKeys);
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

    private static List<string> GetEntityUrls(JToken tweet)
    {
        var entityUrls = new List<string>();

        var legacy = tweet["legacy"];
        var urls = legacy!["entities"]?["urls"];
        if (urls == null)
        {
            return entityUrls;
        }

        foreach (var url in urls)
        {
            var expandedUrl = url["expanded_url"]?.ToString();
            if (expandedUrl != null)
            {
                entityUrls.Add(expandedUrl);
            }
        }
        return entityUrls;
    }

    private static string GetUserId(JToken tweet)
    {
        return tweet.SelectToken(@"$.core.user_results.result.rest_id")?.ToString()
            ?? throw new ArgumentException("Could not get user Id from tweet.", nameof(tweet));
    }

    private static string? GetUserScreenName(JToken tweet)
    {
        return tweet.SelectToken(@"$.core.user_results.result.legacy.screen_name")?.ToString();
    }

    private static string GetLargestSizeMediaUrl(string uri, JToken? sizes)
    {
        if (sizes == null || sizes["large"] == null)
        {
            return uri;
        }

        var match = MediaUrlRegex.Match(uri);
        return match.Success
            ? $"https://pbs.twimg.com/media/{match.Groups["id"].Value}?format={match.Groups["extension"].Value}&name=large"
            : uri;
    }

    private async Task<string> GetUserIdAsync(string userScreenName, CancellationToken cancellationToken)
    {
        IDictionary<string, string>? userIds = null;
        if (Cache != null)
        {
            if (Cache.TryGetValueAs(UserIdCacheKey, out userIds))
            {
                if (userIds?.TryGetValue(userScreenName, out var userIdResult) ?? false)
                {
                    return userIdResult;
                }
            }
            else
            {
                userIds = new Dictionary<string, string>();
                Cache.Set(UserIdCacheKey, userIds);
            }
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

        Logger?.LogDebug("Twitter: Getting user Id for {UserScreenName}.", userScreenName);

        var json = await GetJsonAsync(
            new Uri($"{UserByScreenNameEndpoint}?variables={variables}&features={features}"),
            cancellationToken);

        var userId = json.SelectToken("$.data.user.result.rest_id")!.ToString();
        if (userIds != null)
        {
            userIds[userScreenName] = userId;
        }
        return userId;
    }

    private string? GetUserIdFromCache(string userScreenName)
    {
        if (Cache?.TryGetValueAs<IDictionary<string, string>>(UserIdCacheKey, out var userIds) ?? false)
        {
            if (userIds?.TryGetValue(userScreenName, out var userIdResult) ?? false)
            {
                return userIdResult;
            }
        }
        return null;
    }

    private async Task<string> GetGuestTokenAsync(CancellationToken cancellationToken)
    {
        if (Cache?.TryGetValueAs<string>(GuestTokenCacheKey, out var guestTokenObj) ?? false)
        {
            return guestTokenObj!;
        }

        Logger?.LogDebug("Twitter: Getting guest token");

        using var request = new HttpRequestMessage(HttpMethod.Post, GuestTokenEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", DefaultAuthenticationBearerToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
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

    private async Task<JObject> GetJsonAsync(Uri uri, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", DefaultAuthenticationBearerToken);
        request.Headers.Add("x-guest-token", await GetGuestTokenAsync(cancellationToken));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadAsStringAsync(cancellationToken);
        return JObject.Parse(data);
    }

    private static Uri GetUserTweetsApiUri(string userId, string? cursor)
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

    private static Uri GetTweetApiUri(string tweetId)
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

    private static string GetTweetUri(string userId, string tweetId)
    {
        return $"https://twitter.com/{userId}/status/{tweetId}";
    }

    private static ExtractType? GetExtractType(Uri uri)
    {
        var match = UriRegex.Match(uri.AbsoluteUri);
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

    private static string? GetExtensionFromUri(Uri uri)
    {
        var extension = Path.GetExtension(uri.AbsolutePath);
        return extension.Length > 0 && extension[0] == '.'
            ? extension[1..]
            : extension;
    }

    private static DateTime? ParseDateTime(string datetime)
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
