using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Wilgysef.Stalk.Core.Shared.DateTimeProviders;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Extractors;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;

namespace Wilgysef.Stalk.Extractors.YouTube;

internal class YouTubeCommunityExtractor : YouTubeExtractorBase
{
    private static readonly Regex CommunityImageRegex = new(@"^https://yt3\.ggpht\.com/(?<image>[A-Za-z0-9_-]+)=s(?<size>[0-9]+)", RegexOptions.Compiled);
    private static readonly Regex EmojiImageRegex = new(@"^https://yt3\.ggpht\.com/(?<image>[A-Za-z0-9_-]+)=w(?<width>[0-9]+)-h(?<height>[0-9]+)", RegexOptions.Compiled);

    private static readonly string[] MetadataPostIdKeys = new[] { "post", "id" };
    private static readonly string[] MetadataPostPublishedKeys = new[] { "post", "published" };
    private static readonly string[] MetadataPostPublishedFromKeys = new[] { "post", "published_from" };
    private static readonly string[] MetadataPostVotesKeys = new[] { "post", "votes" };
    private static readonly string[] MetadataPostCommentsCountKeys = new[] { "post", "comments_count" };
    private static readonly string[] MetadataEmojiIdKeys = new[] { "emoji", "id" };
    private static readonly string[] MetadataEmojiSubIdKeys = new[] { "emoji", "sub_id" };
    private static readonly string[] MetadataEmojiNameKeys = new[] { "emoji", "name" };

    public YouTubeCommunityExtractor(
        HttpClient httpClient,
        IDateTimeProvider dateTimeProvider,
        YouTubeExtractorConfig config)
        : base(httpClient, dateTimeProvider, config) { }

    public async IAsyncEnumerable<ExtractResult> ExtractCommunityAsync(
        Uri uri,
        IMetadataObject metadata,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (ExtractorConfig.CommunityEmojisOnly)
        {
            await foreach (var result in ExtractCommunityEmojisOnlyAsync(uri, metadata, cancellationToken))
            {
                yield return result;
            }
            yield break;
        }

        var json = await GetCommunityJsonAsync(uri, cancellationToken);
        var channelId = json.SelectToken("$..channelId")?.ToString()
            ?? json.SelectToken("$..authorEndpoint.browseEndpoint.browseId")!.ToString();

        var isSingle = uri.Query.Length > 0 && HttpUtility.ParseQueryString(uri.Query)["lb"] != null;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var communityPosts = json.SelectTokens("$..backstagePostRenderer");

            foreach (var communityPost in communityPosts)
            {
                foreach (var result in ExtractCommunity(communityPost, metadata))
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

            var continuationTokens = json.SelectTokens("$..continuationCommand.token");
            if (continuationTokens.Count() != 1)
            {
                // TODO: handle?
                break;
            }

            var continuationToken = continuationTokens.First().ToString();
            if (continuationToken == null)
            {
                break;
            }

            json = await GetCommunityJsonAsync(channelId, continuationToken, cancellationToken);
        }
    }

    private async IAsyncEnumerable<ExtractResult> ExtractCommunityEmojisOnlyAsync(
        Uri uri,
        IMetadataObject metadata,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var match = Consts.CommunityPostRegex.Match(uri.AbsoluteUri);
        if (!match.Success)
        {
            var isSingle = uri.Query.Length > 0 && HttpUtility.ParseQueryString(uri.Query)["lb"] != null;
            if (!isSingle)
            {
                uri = await GetFirstCommunityPostUri(uri, cancellationToken);
            }
        }

        var json = await GetCommunityJsonAsync(uri, cancellationToken);
        var channelId = json.SelectToken("$..channelId")?.ToString()
            ?? json.SelectToken("$..authorEndpoint.browseEndpoint.browseId")!.ToString();

        // assume the emojis are one continuation token away from the community post
        // no idea where to find the emojis normally

        var continuationToken = json.SelectToken("$..continuationCommand.token")!.ToString();
        json = await GetCommunityJsonAsync(channelId, continuationToken, cancellationToken);

        foreach (var result in ExtractEmojis(json, metadata, channelId))
        {
            yield return result;
        }
    }

    public static bool IsCommunityExtractType(Uri uri)
    {
        var absoluteUri = uri.AbsoluteUri;
        return Consts.CommunityRegex.IsMatch(absoluteUri) || Consts.CommunityPostRegex.IsMatch(absoluteUri);
    }

    private async Task<Uri> GetFirstCommunityPostUri(Uri communityUri, CancellationToken cancellationToken)
    {
        var json = await GetCommunityJsonAsync(communityUri, cancellationToken);
        var postId = json.SelectTokens("$..postId").First().ToString();
        return new Uri($"https://www.youtube.com/post/{postId}");
    }

    private IEnumerable<ExtractResult> ExtractCommunity(JToken post, IMetadataObject metadata)
    {
        metadata = metadata.Copy();

        var postId = GetMetadata<string>(metadata, post["postId"], MetadataPostIdKeys)!;
        var channelId = GetMetadata<string>(metadata, post.SelectToken("$..authorEndpoint.browseEndpoint.browseId"), MetadataChannelIdKeys)!;
        GetMetadata<string>(metadata, post.SelectToken("$..authorText.runs[*].text"), MetadataChannelNameKeys);

        var publishedTime = post.SelectToken("$.publishedTimeText.runs[*].text");
        string? relativeDateTime = null;
        if (publishedTime != null)
        {
            relativeDateTime = GetRelativeDateTime(publishedTime.ToString());
            metadata[MetadataPostPublishedKeys] = relativeDateTime;
            metadata[MetadataPostPublishedFromKeys] = publishedTime.ToString() + $" from {DateTimeProvider.OffsetNow}";
        }

        GetMetadata<string>(metadata, post["voteCount"]?["simpleText"], MetadataPostVotesKeys);
        GetMetadata<string>(metadata, post.SelectToken("$..replyButton.buttonRenderer.text.simpleText"), MetadataPostCommentsCountKeys);

        var imageResult = ExtractCommunityImage(post, metadata, relativeDateTime);
        if (imageResult != null)
        {
            yield return imageResult;
        }

        var textResult = ExtractCommunityText(
            post,
            postId,
            channelId,
            relativeDateTime,
            metadata);
        if (textResult != null)
        {
            yield return textResult;
        }
    }

    private static ExtractResult? ExtractCommunityText(
        JToken post,
        string postId,
        string channelId,
        string? relativeDateTime,
        IMetadataObject metadata)
    {
        metadata = metadata.Copy();

        var contextTextRuns = post["contentText"]?["runs"];
        if (contextTextRuns == null)
        {
            return null;
        }

        if (relativeDateTime != null)
        {
            metadata[MetadataObjectConsts.Origin.ItemIdSeqKeys] = $"{channelId}#community#{relativeDateTime}_{postId}";
        }

        var textBuilder = new StringBuilder();
        foreach (var content in contextTextRuns)
        {
            var url = GetContentRunsUrl(content);
            if (url != null)
            {
                textBuilder.Append(url);
            }
            else if (content["text"] != null)
            {
                textBuilder.Append(content["text"]!.ToString());
            }
        }
        var text = textBuilder.ToString();

        metadata[MetadataObjectConsts.File.ExtensionKeys] = "txt";

        var canonicalBaseUrl = post.SelectToken("$..publishedTimeText..canonicalBaseUrl")?.ToString();
        metadata[MetadataObjectConsts.Origin.UriKeys] = canonicalBaseUrl != null
            ? $"https://www.youtube.com{canonicalBaseUrl}"
            : $"https://www.youtube.com/post/{postId}";

        return new ExtractResult(
            Encoding.UTF8.GetBytes(text),
            mediaType: "text/plain;charset=UTF-8",
            itemId: postId,
            metadata: metadata);
    }

    private static ExtractResult? ExtractCommunityImage(JToken post, IMetadataObject metadata, string? relativeDateTime)
    {
        metadata = metadata.Copy();

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

        metadata[MetadataObjectConsts.File.ExtensionKeys] = "png";

        if (relativeDateTime != null)
        {
            metadata[MetadataObjectConsts.Origin.ItemIdSeqKeys] = $"{channelId}#community#{relativeDateTime}_{postId}#image";
        }

        imageUrl = GetCommunityImageUrlFromThumbnail(imageUrl);

        return new ExtractResult(
            imageUrl,
            $"{postId}#image",
            JobTaskType.Download,
            metadata: metadata);
    }

    private IEnumerable<ExtractResult> ExtractEmojis(JObject json, IMetadataObject metadata, string? channelId = null)
    {
        var customEmojis = json.SelectToken("$..customEmojis");
        if (customEmojis == null)
        {
            yield break;
        }

        foreach (var emoji in customEmojis)
        {
            var result = ExtractEmoji(emoji, metadata, channelId);
            if (result != null)
            {
                yield return result;
            }
        }
    }

    private ExtractResult? ExtractEmoji(JToken emoji, IMetadataObject metadata, string? channelId = null)
    {
        metadata = metadata.Copy();

        var emojiId = emoji["emojiId"]!.ToString();
        var emojiIdParts = emojiId.Split("/");
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

        metadata[MetadataChannelIdKeys] = emojiChannelId;
        metadata[MetadataEmojiIdKeys] = emojiId;
        metadata[MetadataEmojiSubIdKeys] = emojiSubId;
        GetMetadata<string>(metadata, emoji.SelectToken("$..label"), MetadataEmojiNameKeys);
        metadata[MetadataObjectConsts.File.ExtensionKeys] = "png";

        return new ExtractResult(
            GetScaledEmojiImageUri(url),
            $"{emojiChannelId}#emoji#{emojiSubId}",
            JobTaskType.Download,
            metadata: metadata);
    }

    private static string GetCommunityImageUrlFromThumbnail(string url)
    {
        var match = CommunityImageRegex.Match(url);
        return match.Success
            ? $"https://yt3.ggpht.com/{match.Groups["image"].Value}=s{match.Groups["size"].Value}-nd-v1"
            : url;
    }

    private string GetScaledEmojiImageUri(string url)
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

        if (width < ExtractorConfig.EmojiScaleWidth)
        {
            var mult = (float)ExtractorConfig.EmojiScaleWidth / width;
            width = ExtractorConfig.EmojiScaleWidth;
            height = (int)(height * mult);
        }

        return $"https://yt3.ggpht.com/{match.Groups["image"].Value}=w{width}-h{height}-c-k-nd";
    }

    private async Task<JObject> GetCommunityJsonAsync(Uri uri, CancellationToken cancellationToken)
    {
        return await GetYtInitialData(uri, cancellationToken);
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

    private static string? GetContentRunsUrl(JToken content)
    {
        var redirectUrl = content["navigationEndpoint"]?["urlEndpoint"]?["url"]?.ToString();
        if (redirectUrl == null)
        {
            return null;
        }

        try
        {
            var uri = new Uri(redirectUrl);
            var query = HttpUtility.ParseQueryString(uri.Query);
            var q = query["q"];
            return q;
        }
        catch
        {
            return null;
        }
    }
}
