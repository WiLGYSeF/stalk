using Wilgysef.Stalk.Core.Shared.Extensions;

namespace Wilgysef.Stalk.Extractors.YouTube;

public class YouTubeExtractorConfig : YouTubeConfig
{
    public static readonly string CookiesKey = "cookies";
    public static readonly string UseWebpThumbnailsKey = "useWebpThumbnails";
    public static readonly string EmojiScaleWidthKey = "emojiScaleWidth";
    public static readonly string CommunityEmojisOnlyKey = "communityEmojisOnly";
    public static readonly string YouTubeClientVersionKey = "youtubeClientVersion";

    public string? CookieString { get; set; }

    public bool UseWebpThumbnails { get; set; } = true;

    // NOTE: custom emojis are normally downloaded at 24x24 by the browser
    public int EmojiScaleWidth { get; set; } = 512;

    public bool CommunityEmojisOnly { get; set; } = false;

    public string YouTubeClientVersion { get; set; } = "2.20220929.09.00";

    public YouTubeExtractorConfig() { }

    public YouTubeExtractorConfig(IDictionary<string, object?>? config)
    {
        if (config?.TryGetValueAs<IEnumerable<string>, string, object?>(CookiesKey, out var cookies) ?? false)
        {
            CookieString = GetCookieString(cookies);
        }
        else if (config?.TryGetValueAs<string, string, object?>(CookiesKey, out var cookie) ?? false)
        {
            CookieString = GetCookieString(new[] { cookie });
        }

        if (config?.TryGetValueAs<bool, string, object?>(UseWebpThumbnailsKey, out var useWebpThumbnails) ?? false)
        {
            UseWebpThumbnails = useWebpThumbnails;
        }
        if (config?.TryGetValueAs<int, string, object?>(EmojiScaleWidthKey, out var emojiScaleWidth) ?? false)
        {
            EmojiScaleWidth = emojiScaleWidth;
        }
        if (config?.TryGetValueAs<bool, string, object?>(CommunityEmojisOnlyKey, out var communityEmojisOnly) ?? false)
        {
            CommunityEmojisOnly = communityEmojisOnly;
        }
        if (config?.TryGetValueAs<string, string, object?>(YouTubeClientVersion, out var youtubeClientVersion) ?? false)
        {
            YouTubeClientVersion = youtubeClientVersion;
        }
    }
}
