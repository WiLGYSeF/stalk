using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Extractors;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;

namespace Wilgysef.Stalk.TwitterExtractors;

public class TwitterExtractor : IExtractor
{
    private const string UserByScreenNameEndpoint = "https://twitter.com/i/api/graphql/vG3rchZtwqiwlKgUYCrTRA/UserByScreenName";
    private const string UserTweetsEndpoint = "https://twitter.com/i/api/graphql/q881FFtQa69KN7jS9h_EDA/UserTweets";
    private const string TweetDetailEndpoint = "https://twitter.com/i/api/graphql/Nze3idtpjn4wcl09GpmDRg/TweetDetail";

    public string Name => "Twitter";

    public ILogger? Logger { get; set; }

    private readonly Regex _uriRegex = new(@"^(?:https?://)?(?:www\.)?twitter\.com(?:\:(?:80|443))?/(?<user>[^/]+)(?:/status/(?<tweet>[0-9]+))?", RegexOptions.Compiled);
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

    public IAsyncEnumerable<ExtractResult> ExtractAsync(Uri uri, string itemData, IMetadataObject metadata, CancellationToken cancellationToken = default)
    {
        switch (GetExtractType(uri))
        {
            case ExtractType.User:
                return ExtractUserAsync(uri, itemData, metadata, cancellationToken);
            case ExtractType.Tweet:
                return ExtractTweetAsync(uri, itemData, metadata, cancellationToken);
            default:
                throw new ArgumentException("Cannot extract URI.", nameof(uri));
        }
    }

    public void SetHttpClient(HttpClient client)
    {
        _httpClient?.Dispose();
        _httpClient = client;
    }

    private async IAsyncEnumerable<ExtractResult> ExtractUserAsync(Uri uri, string itemData, IMetadataObject metadata, CancellationToken cancellationToken = default)
    {
        var match = _uriRegex.Match(uri.AbsoluteUri);
        var userScreenName = match.Groups["user"].Value;

        var userId = await GetUserIdAsync(userScreenName);
        string? cursor = null;

        while (true)
        {
            var userTweetsUri = GetUserTweetsUri(userId, cursor);
            var response = await _httpClient.GetAsync(userTweetsUri);
            response.EnsureSuccessStatusCode();
        }

        // TODO

        yield return new ExtractResult(new Uri(""), "", JobTaskType.Extract);
    }

    private async IAsyncEnumerable<ExtractResult> ExtractTweetAsync(Uri uri, string itemData, IMetadataObject metadata, CancellationToken cancellationToken = default)
    {
        var match = _uriRegex.Match(uri.AbsoluteUri);
        var tweetId = match.Groups["tweet"].Value;

        var tweetUri = GetTweetUri(tweetId);
        var response = await _httpClient.GetAsync(tweetUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadAsStringAsync();
        var jObject = JObject.Parse(data);

        var tweet = jObject.SelectToken($"$.data.threaded_conversation_with_injections_v2.instructions[?(@.type=='TimelineAddEntries')].entries..[?(@.rest_id=='{tweetId}')]");

        var userScreenName = tweet.SelectToken(@"$.core.user_results.result.legacy.screen_name");
        var legacy = tweet["legacy"];
        metadata.AddValue("created_at", legacy["created_at"]?.ToString());
        metadata.AddValue("favorite_count", legacy["favorite_count"]?.Value<int>());
        metadata.AddValue("lang", legacy["lang"]?.ToString());
        metadata.AddValue("possibly_sensitive", legacy["possibly_sensitive"]?.Value<bool>());
        metadata.AddValue("quote_count", legacy["quote_count"]?.Value<int>());
        metadata.AddValue("reply_count", legacy["reply_count"]?.Value<int>());
        metadata.AddValue("retweet_count", legacy["retweet_count"]?.Value<int>());

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

            if (fullText.Length > 0)
            {
                yield return new ExtractResult(
                    new Uri($"data:text/plain;base64,{Convert.ToBase64String(Encoding.UTF8.GetBytes(fullText))}"),
                    $"{userScreenName}#{tweetId}",
                    JobTaskType.Download,
                    metadata: metadata);
            }
        }
    }

    private IEnumerable<ExtractResult> ExtractTweetMedia(JToken tweet, IMetadataObject metadata, bool largestSize)
    {
        var userScreenName = tweet.SelectToken(@"$.core.user_results.result.legacy.screen_name");
        var legacy = tweet["legacy"];
        var tweetId = legacy["id_str"]?.ToString();
        var entities = legacy["entities"];
        if (entities == null)
        {
            yield break;
        }

        var media = entities["media"];
        if (media == null)
        {
            yield break;
        }

        foreach (var mediaItem in media)
        {
            var mediaUrl = mediaItem["media_url_https"]?.ToString();
            var mediaId = mediaItem["id_str"]?.ToString();
            if (mediaUrl == null || mediaId == null)
            {
                continue;
            }

            if (largestSize)
            {
                mediaUrl = GetLargestSizeMediaUrl(mediaUrl, mediaItem["sizes"]);
            }

            yield return new ExtractResult(
                new Uri(mediaUrl),
                $"{userScreenName}#{tweetId}#{mediaId}",
                JobTaskType.Download,
                metadata: metadata);
        }
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

    private async Task<string> GetUserIdAsync(string userScreenName)
    {
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
        var response = await _httpClient.GetAsync(uri);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadAsStringAsync();
        var jObject = JObject.Parse(data);

        var userId = jObject.SelectToken("$.data.user.result.rest_id")!.ToString();
        return userId;
    }

    private Uri GetUserTweetsUri(string userId, string? cursor)
    {
        dynamic variablesObject = new
        {
            userId = userId,
            count = 40,
            includePromotedContent = true,
            withQuickPromoteEligibilityTweetFields = true,
            withSuperFollowsUserFields = true,
            withDownvotePerspective = false,
            withReactionsMetadata = false,
            withReactionsPerspective = false,
            withSuperFollowsTweetFields = true,
            withVoice = true,
            withV2Timeline = true,
        };

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

    private enum ExtractType
    {
        User,
        Tweet,
    }
}
