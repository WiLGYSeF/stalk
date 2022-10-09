using System.Text;
using Wilgysef.Stalk.Core.Shared.Extensions;

namespace Wilgysef.Stalk.Extractors.YouTube;

public class YouTubeExtractorConfig : YouTubeConfig
{
    public static readonly string CookiesKey = "cookies";
    public static readonly string UseWebpThumbnailsKey = "useWebpThumbnails";

    public string? CookieString { get; set; }

    public bool UseWebpThumbnails { get; set; } = true;

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
    }
}
