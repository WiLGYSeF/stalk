using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Wilgysef.Stalk.Core.Shared.Extractors;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;

namespace Wilgysef.Stalk.TwitterExtractors;

public class TwitterExtractor : IExtractor
{
    public string Name => "Twitter";

    public ILogger? Logger { get; set; }

    private readonly Regex _uriRegex = new(@"^(?:https?://)?(?:www\.)?twitter\.com(?:\:(?:80|443))?/(?<user>[^/]+)(?:/status/(?<tweet>[0-9]+))?", RegexOptions.Compiled);

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
    }

    private async IAsyncEnumerable<ExtractResult> ExtractTweetAsync(Uri uri, string itemData, IMetadataObject metadata, CancellationToken cancellationToken = default)
    {
        var match = _uriRegex.Match(uri.AbsoluteUri);
        var tweetId = match.Groups["tweet"].Value;

        var tweetUri = GetTweetUri(tweetId);
        var response = await _httpClient.GetAsync(tweetUri, cancellationToken);
        response.EnsureSuccessStatusCode();
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

        var uri = new Uri($"https://twitter.com/i/api/graphql/vG3rchZtwqiwlKgUYCrTRA/UserByScreenName?variables={variables}&features={features}");
        var response = await _httpClient.GetAsync(uri);
        response.EnsureSuccessStatusCode();
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

        return new Uri($"https://twitter.com/i/api/graphql/q881FFtQa69KN7jS9h_EDA/UserTweets?variables={variables}&features={features}");
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

        return new Uri($"https://twitter.com/i/api/graphql/Nze3idtpjn4wcl09GpmDRg/TweetDetail?variables={variables}&features={features}");
    }

    private ExtractType? GetExtractType(Uri uri)
    {

    }

    private enum ExtractType
    {
        User,
        Tweet,
    }
}
