using Shouldly;
using System.Diagnostics;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Wilgysef.HttpClientInterception;
using Wilgysef.Stalk.Core.FilenameSlugs;
using Wilgysef.Stalk.Core.MetadataObjects;
using Wilgysef.Stalk.Core.MetadataSerializers;
using Wilgysef.Stalk.Core.Shared.Options;
using Wilgysef.Stalk.Core.StringFormatters;
using Wilgysef.Stalk.Extractors.TestBase;
using Wilgysef.Stalk.TestBase.Shared.Mocks;

namespace Wilgysef.Stalk.Extractors.YouTube.Tests;

public class YouTubeDownloaderTest : BaseTest
{
    private static readonly string MockedDataResourcePrefix = $"{typeof(YouTubeDownloaderTest).Namespace}.MockedData";

    private readonly MockProcessService _processService;
    private readonly YouTubeDownloader _youTubeDownloader;

    public YouTubeDownloaderTest()
    {
        var interceptor = HttpClientInterceptor.Create();
        interceptor.AddForAny(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var assembly = Assembly.GetExecutingAssembly();

        var fileSystem = new MockFileSystem();
        fileSystem.AddFileFromEmbeddedResource(
            Path.Combine(Directory.GetCurrentDirectory(), "UCdYR5Oyz8Q4g0ZmB4PkTD7g#video#20211227_2SVDVhzzzSY.info.json"),
            assembly,
            $"{MockedDataResourcePrefix}.Video.2SVDVhzzzSY.info.json");

        _processService = new MockProcessService();
        _processService.For(new Regex(".*"), builder =>
        {
            builder.WithStartInfo(new ProcessStartInfo
            {
                FileName = "",
                WorkingDirectory = "",
            });
            builder.WithStandardOutput(
                new StreamReader(
                    assembly.GetManifestResourceStream($"{MockedDataResourcePrefix}.YoutubeDl.2SVDVhzzzSY.txt")!));
        });

        _youTubeDownloader = new YouTubeDownloader(
            fileSystem,
            new StringFormatter(),
            new FilenameSlugSelector(new[] { new WindowsFilenameSlug() }),
            new MetadataSerializer(),
            new ExternalBinariesOptions
            {
                Path = @"D:\projects\stalk\Wilgysef.Stalk\ExternalBinaries\"
            },
            _processService,
            new HttpClient(interceptor));
    }

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=2SVDVhzzzSY", true)]
    [InlineData("https://youtube.com/watch?v=2SVDVhzzzSY", true)]
    public void Can_Download(string uri, bool expected)
    {
        _youTubeDownloader.CanDownload(new Uri(uri)).ShouldBe(expected);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Download_With_Subtitle_InfoJson(bool moveInfoJson)
    {
        _youTubeDownloader.Config[YouTubeDownloaderConfig.MoveInfoJsonToMetadataKey] = moveInfoJson;

        var results = await _youTubeDownloader.DownloadAsync(
            new Uri("https://www.youtube.com/watch?v=2SVDVhzzzSY"),
            "UCdYR5Oyz8Q4g0ZmB4PkTD7g#video#20211227_2SVDVhzzzSY.%(ext)s",
            "2SVDVhzzzSY",
            "meta.txt",
            new MetadataObject()).ToListAsync();

        results.Count.ShouldBe(moveInfoJson ? 2 : 3);
        var subtitlesResult = results.Single(r => r.Path.EndsWith("UCdYR5Oyz8Q4g0ZmB4PkTD7g#video#20211227_2SVDVhzzzSY.ja.vtt"));
        var videoResult = results.Single(r => r.Path.EndsWith("UCdYR5Oyz8Q4g0ZmB4PkTD7g#video#20211227_2SVDVhzzzSY.webm"));

        if (moveInfoJson)
        {
            ((IDictionary<string, object>)(videoResult.Metadata!["youtube_dl"]!)).Count.ShouldBe(56);
        }
        else
        {
            var metadataResult = results.Single(r => r.Path.EndsWith(".info.json"));
            videoResult.Metadata!.Contains("youtube_dl").ShouldBeFalse();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Download_With_Cookies(bool multipleCookies)
    {
        var cookies = new Dictionary<string, string>();
        if (multipleCookies)
        {
            cookies["YSC"] = "_wwCnBu9guc";
            cookies["VISITOR_INFO1_LIVE"] = "MHllVv46iSU";
            _youTubeDownloader.Config[YouTubeDownloaderConfig.CookiesKey] = cookies.Select(p => $"{p.Key}={p.Value}");
        }
        else
        {
            cookies["VISITOR_INFO1_LIVE"] = "MHllVv46iSU";
            _youTubeDownloader.Config[YouTubeDownloaderConfig.CookiesKey] = cookies.Select(p => $"{p.Key}={p.Value}").Single();
        }

        var results = await _youTubeDownloader.DownloadAsync(
            new Uri("https://www.youtube.com/watch?v=2SVDVhzzzSY"),
            "test.%(ext)s",
            "itemId",
            "meta.txt",
            new MetadataObject()).ToListAsync();

        var process = _processService.Processes.Single();

        using var enumerator = process.StartInfo.ArgumentList.GetEnumerator();
        string? cookieHeader = null;

        while (enumerator.MoveNext())
        {
            if (enumerator.Current == "--add-header")
            {
                enumerator.MoveNext();
                cookieHeader = enumerator.Current;
                break;
            }
        }

        cookieHeader.ShouldBe("Cookie:" + string.Join("; ", cookies.Select(p => $"{p.Key}={p.Value}")));
    }
}
