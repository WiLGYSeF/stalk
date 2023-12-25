using System.Linq.Expressions;
using Wilgysef.Stalk.Core.Shared.Extensions;

namespace Wilgysef.Stalk.Extractors.YouTube;

public class YouTubeExtractorConfig
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
        TrySetValue(() => CookieString, config, CookiesKey);
        TrySetValue(() => UseWebpThumbnails, config, UseWebpThumbnailsKey);
        TrySetValue(() => EmojiScaleWidth, config, EmojiScaleWidthKey);
        TrySetValue(() => CommunityEmojisOnly, config, CommunityEmojisOnlyKey);
        TrySetValue(() => YouTubeClientVersion, config, YouTubeClientVersionKey);
    }

    // TODO: move to shared or replace
    private static bool TrySetValue<T>(Expression<Func<T>> property, IDictionary<string, object?>? config, string key)
    {
        if (!(config?.TryGetValueAs<T, string, object?>(key, out var value) ?? false))
        {
            return false;
        }

        Expression.Lambda(Expression.Assign(property.Body, Expression.Constant(value)))
            .Compile()
            .DynamicInvoke();
        return true;
    }
}
