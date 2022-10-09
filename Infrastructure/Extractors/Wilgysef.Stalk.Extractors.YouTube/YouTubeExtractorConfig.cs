using System.Text;
using Wilgysef.Stalk.Core.Shared.Extensions;

namespace Wilgysef.Stalk.Extractors.YouTube;

public class YouTubeExtractorConfig
{
    public static string CookiesKey = "cookies";
    public static string UseWebpThumbnailsKey = "useWebpThumbnails";

    public string? CookieString { get; }

    public bool UseWebpThumbnails { get; } = true;

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

    private static string GetCookieString(IEnumerable<string> cookies)
    {
        var builder = new StringBuilder();

        foreach (var cookie in cookies)
        {
            builder.Append(cookie.TrimEnd(';', ' '));
            builder.Append("; ");
        }

        if (builder.Length != 0)
        {
            builder.Remove(builder.Length - 2, 2);
        }
        return builder.ToString();
    }
}
