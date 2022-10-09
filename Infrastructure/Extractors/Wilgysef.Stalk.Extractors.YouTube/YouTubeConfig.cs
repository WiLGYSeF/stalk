using System.Text;

namespace Wilgysef.Stalk.Extractors.YouTube;

public abstract class YouTubeConfig
{
    protected static string GetCookieString(IEnumerable<string> cookies)
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
