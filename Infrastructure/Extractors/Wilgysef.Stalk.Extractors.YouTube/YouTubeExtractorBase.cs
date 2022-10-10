﻿using Newtonsoft.Json.Linq;
using System.Net.Http.Json;
using HtmlAgilityPack;

namespace Wilgysef.Stalk.Extractors.YouTube;

public abstract class YouTubeExtractorBase
{
    protected static readonly string[] MetadataChannelIdKeys = new string[] { "channel", "id" };
    protected static readonly string[] MetadataChannelNameKeys = new string[] { "channel", "name" };
    protected static readonly string[] MetadataVideoIdKeys = new string[] { "video", "id" };
    protected static readonly string[] MetadataVideoTitleKeys = new string[] { "video", "title" };
    protected static readonly string[] MetadataVideoDurationKeys = new string[] { "video", "duration" };
    protected static readonly string[] MetadataVideoDurationSecondsKeys = new string[] { "video", "duration_seconds" };

    public IDictionary<string, object?> Config { get; set; } = new Dictionary<string, object?>();

    protected HttpClient HttpClient { get; set; }

    protected YouTubeExtractorConfig ExtractorConfig { get; set; }

    protected YouTubeExtractorBase(HttpClient httpClient) : this(httpClient, new()) { }

    protected YouTubeExtractorBase(HttpClient httpClient, YouTubeExtractorConfig config)
    {
        HttpClient = httpClient;
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
                    originalUrl = originalUrl,
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
        doc.Load(response.Content.ReadAsStream(cancellationToken));
        return doc;
    }

    protected HttpRequestMessage ConfigureRequest(HttpRequestMessage request)
    {
        if (ExtractorConfig.CookieString != null)
        {
            request.Headers.Add("Cookie", ExtractorConfig.CookieString);
        }
        return request;
    }

    protected static DateTime? GetDateTime(string? dateTime)
    {
        if (dateTime == null)
        {
            return null;
        }

        var streamedLiveOn = "Streamed live on ";
        if (dateTime.StartsWith(streamedLiveOn))
        {
            dateTime = dateTime[streamedLiveOn.Length..];
        }

        var success = DateTime.TryParse(dateTime, out var result);
        if (success)
        {
            return result;
        }

        var streamed = "Streamed ";
        if (dateTime.StartsWith(streamed))
        {
            dateTime = dateTime[streamed.Length..];
        }

        var relative = GetRelativeDateTime(dateTime);
        return relative != null ? DateTime.Parse(relative) : null;
    }

    protected static string? GetRelativeDateTime(string relative, DateTime? dateTime = null)
    {
        var split = relative.Split(" ");
        if (split.Length != 3 || !int.TryParse(split[0], out var number) || split[2] != "ago")
        {
            return null;
        }

        DateTime relativeTime;
        var span = split[1];
        dateTime ??= DateTime.Now;

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
        }
        else if (span.StartsWith("year"))
        {
            relativeTime = dateTime.Value.AddYears(-number);
        }
        else
        {
            return null;
        }

        return relativeTime.ToString("yyyyMMdd");
    }
}