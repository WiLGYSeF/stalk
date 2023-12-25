using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Wilgysef.Stalk.Core.Shared.DateTimeProviders;
using Wilgysef.Stalk.Core.Shared.Extensions;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;

namespace Wilgysef.Stalk.Extractors.YouTube;

public abstract class YouTubeExtractorBase
{
    private const string OriginUrl = "https://www.youtube.com";

    protected static readonly string[] MetadataChannelIdKeys = new string[] { "channel", "id" };
    protected static readonly string[] MetadataChannelNameKeys = new string[] { "channel", "name" };

    public IDictionary<string, object?> Config { get; set; } = new Dictionary<string, object?>();

    protected HttpClient HttpClient { get; set; }

    protected YouTubeExtractorConfig ExtractorConfig { get; set; }

    protected IDateTimeProvider DateTimeProvider { get; set; }

    protected YouTubeExtractorBase(HttpClient httpClient, IDateTimeProvider dateTimeProvider)
        : this(httpClient, dateTimeProvider, new()) { }

    protected YouTubeExtractorBase(
        HttpClient httpClient,
        IDateTimeProvider dateTimeProvider,
        YouTubeExtractorConfig config)
    {
        HttpClient = httpClient;
        DateTimeProvider = dateTimeProvider;
        ExtractorConfig = config;
    }

    public void SetHttpClient(HttpClient client)
    {
        HttpClient = client;
    }

    protected async Task<JObject> GetBrowseJsonAsync(
        string originalUrl,
        string continuationToken,
        CancellationToken cancellationToken)
    {
        var data = new
        {
            context = new
            {
                adSignalsInfo = new
                {
                    @params = Array.Empty<object>()
                },
                clickTracking = new { },
                client = new
                {
                    clientFormFactor = "UNKNOWN_FORM_FACTOR",
                    clientName = "WEB",
                    clientVersion = ExtractorConfig.YouTubeClientVersion,
                    deviceMake = "",
                    deviceModel = "",
                    gl = "US",
                    hl = "en",
                    mainAppWebInfo = new
                    {
                        graftUrl = originalUrl,
                        isWebNativeShareAvailable = false,
                        webDisplayMode = "WEB_DISPLAY_MODE_BROWSER"
                    },
                    originalUrl,
                    platform = "DESKTOP",
                    userInterfaceTheme = "USER_INTERFACE_THEME_DARK",
                },
                request = new
                {
                    consistencyTokenJars = Array.Empty<object>(),
                    internalExperimentFlags = Array.Empty<object>(),
                    useSsl = true
                },
                user = new
                {
                    lockedSafetyMode = false
                }
            },
            continuation = continuationToken
        };

        // TODO: replace with INNERTUBE_API_KEY
        var request = ConfigureRequest(new HttpRequestMessage(HttpMethod.Post, "https://www.youtube.com/youtubei/v1/browse?key=AIzaSyAO_FJ2SlqU8Q4STEHLGCilw_Y9_11qcW8&prettyPrint=false"));
        request.Content = JsonContent.Create(data);

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return JObject.Parse(content);
    }

    protected async Task<JObject> GetYtInitialData(Uri uri, CancellationToken cancellationToken)
    {
        return GetYtInitialData(await GetHtmlDocument(uri, cancellationToken));
    }

    protected static JObject GetYtInitialData(HtmlDocument doc)
    {
        var scripts = doc.DocumentNode.SelectNodes("//script");

        var prefix = "var ytInitialData = ";
        var initialData = scripts.Single(n => n.InnerHtml.TrimStart().StartsWith(prefix));
        var trimmedHtml = initialData.InnerHtml.Trim();
        var json = trimmedHtml.Substring(prefix.Length, trimmedHtml.Length - prefix.Length - 1);

        return JObject.Parse(json)
            ?? throw new ArgumentException("Could not get initial playlist data.", nameof(doc));
    }

    protected async Task<HtmlDocument> GetHtmlDocument(Uri uri, CancellationToken cancellationToken)
    {
        var request = ConfigureRequest(new HttpRequestMessage(HttpMethod.Get, uri));
        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var doc = new HtmlDocument();
        doc.Load(await response.Content.ReadAsStreamAsync(cancellationToken));
        return doc;
    }

    protected HttpRequestMessage ConfigureRequest(HttpRequestMessage request)
    {
        if (ExtractorConfig.CookieString != null)
        {
            KeyValuePair<string, string>? sapisidCookie = GetCookies(ExtractorConfig.CookieString).FirstOrDefault(p => p.Key == "SAPISID");
            if (sapisidCookie.HasValue)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("SAPISIDHASH", GetSapisidhash(sapisidCookie.Value.Value));
            }

            request.Headers.Add("Cookie", ExtractorConfig.CookieString);
        }

        request.Headers.Add("X-Origin", OriginUrl);
        return request;
    }

    protected static IEnumerable<KeyValuePair<string, string>> GetCookies(string cookies)
    {
        return cookies.Split(";").Select(c => Separate(c.TrimStart(), '='));
    }

    protected string GetSapisidhash(string sapisid)
    {
        // thank you
        // https://stackoverflow.com/a/32065323

        var epochSeconds = DateTimeProvider.OffsetNow.ToUnixTimeSeconds();
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes($"{epochSeconds} {sapisid} {OriginUrl}"));

        return $"{epochSeconds}_{hash.ToHexString()}";
    }

    protected static T? GetMetadata<T>(IMetadataObject metadata, JToken? token, params string[] keys)
    {
        if (token == null)
        {
            return default;
        }

        var value = token.Value<T>();
        metadata[keys] = value;
        return value;
    }

    protected DateTime? GetDateTime(string? dateTime)
    {
        if (dateTime == null)
        {
            return null;
        }

        dateTime = SanitizeDateTime(dateTime);

        var success = DateTime.TryParse(dateTime, out var result);
        if (success)
        {
            return result;
        }

        var relative = GetRelativeDateTime(dateTime);
        return relative != null ? DateTime.Parse(relative) : null;
    }

    protected string? GetRelativeDateTime(string relative, DateTime? dateTime = null)
    {
        var split = SanitizeDateTime(relative).Split(" ");
        if (split.Length != 3 || !int.TryParse(split[0], out var number) || split[2] != "ago")
        {
            return null;
        }

        DateTime relativeTime;
        var span = split[1];
        dateTime ??= DateTimeProvider.Now;

        if (span.StartsWith("second"))
        {
            relativeTime = dateTime.Value.AddSeconds(-number);
        }
        else if (span.StartsWith("minute"))
        {
            relativeTime = dateTime.Value.AddMinutes(-number);
        }
        else if (span.StartsWith("hour"))
        {
            relativeTime = dateTime.Value.AddHours(-number);
        }
        else if (span.StartsWith("day"))
        {
            relativeTime = dateTime.Value.AddDays(-number);
        }
        else if (span.StartsWith("week"))
        {
            relativeTime = dateTime.Value.AddDays(-number * 7);
        }
        else if (span.StartsWith("month"))
        {
            relativeTime = dateTime.Value.AddMonths(-number);
            relativeTime = new DateTime(relativeTime.Year, relativeTime.Month, 1, 0, 0, 0);
        }
        else if (span.StartsWith("year"))
        {
            relativeTime = dateTime.Value.AddYears(-number);
            relativeTime = new DateTime(relativeTime.Year, 1, 1, 0, 0, 0);
        }
        else
        {
            return null;
        }

        return relativeTime.ToString("yyyyMMdd");
    }

    protected static string SanitizeDateTime(string dateTime)
    {
        var streamedLiveOn = "Streamed live on ";
        if (dateTime.StartsWith(streamedLiveOn))
        {
            dateTime = dateTime[streamedLiveOn.Length..];
        }

        var streamed = "Streamed ";
        if (dateTime.StartsWith(streamed))
        {
            dateTime = dateTime[streamed.Length..];
        }

        var premiered = "Premiered ";
        if (dateTime.StartsWith(premiered))
        {
            dateTime = dateTime[premiered.Length..];
        }

        var editedIndex = dateTime.IndexOf(" (edited)");
        if (editedIndex != -1)
        {
            dateTime = dateTime[..editedIndex];
        }

        return dateTime;
    }

    private static KeyValuePair<string, string> Separate(string value, char separator)
    {
        var index = value.IndexOf(separator);
        return new KeyValuePair<string, string>(value[..index], value[(index + 1)..]);
    }
}
