using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using Wilgysef.Stalk.Core.Shared.CacheObjects;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Extractors;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;

namespace Wilgysef.Stalk.Extractors.YouTube;

internal class YouTubeCommunityExtractor : YouTubeExtractorBase
{
    private static readonly Regex CommunityImageRegex = new(@"^https://yt3\.ggpht\.com/(?<image>[A-Za-z0-9_-]+)=s(?<size>[0-9]+)", RegexOptions.Compiled);
    private static readonly Regex EmojiImageRegex = new(@"^https://yt3\.ggpht\.com/(?<image>[A-Za-z0-9_-]+)=w(?<width>[0-9]+)-h(?<height>[0-9]+)", RegexOptions.Compiled);

    private readonly ILogger? _logger;
    private readonly ICacheObject<string, object?>? _cache;

    public YouTubeCommunityExtractor(
        HttpClient httpClient,
        YouTubeExtractorConfig config,
        ILogger? logger,
        ICacheObject<string, object?>? cache)
            : base(httpClient, config)
    {
        _logger = logger;
        _cache = cache;
    }

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
        // I have no idea where to find the emojis normally

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

        metadata.SetByParts("txt", MetadataObjectConsts.File.ExtensionKeys);

        var canonicalBaseUrl = post.SelectToken("$..publishedTimeText..canonicalBaseUrl")?.ToString();
        metadata.SetByParts(
            canonicalBaseUrl != null
                ? $"https://www.youtube.com{canonicalBaseUrl}"
                : $"https://www.youtube.com/post/{postId}",
            MetadataObjectConsts.Origin.UriKeys);

        return new ExtractResult(
            Encoding.UTF8.GetBytes(text),
            mediaType: "text/plain;charset=UTF-8",
            itemId: $"{channelId}#community#{postId}",
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
            imageUrl,
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
