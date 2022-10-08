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
            Path.Combine(Directory.GetCurrentDirectory(), "output\\UCdYR5Oyz8Q4g0ZmB4PkTD7g#video#20211227_2SVDVhzzzSY.info.json"),
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

    [Fact]
    public async Task Download_With_Subtitle()
    {
        var results = await _youTubeDownloader.DownloadAsync(
            new Uri("https://www.youtube.com/watch?v=2SVDVhzzzSY"),
            "test.%(ext)s",
            "itemId",
            "meta.txt",
            new MetadataObject('.')).ToListAsync();

        results.Count.ShouldBe(2);
        var subtitlesResult = results.Single(r => r.Path.EndsWith(".vtt"));
        var videoResult = results.Single(r => r.Path.EndsWith(".webm"));
        videoResult.Metadata!["file.size"].ShouldBeNull();
        ((IDictionary<object, object>)(videoResult.Metadata["youtube"]!)).Count.ShouldBe(56);
    }

    [Fact]
    public async Task Download_With_Subtitle_InfoMetadata()
    {
        _youTubeDownloader.MoveInfoJsonToMetadata = false;

        var results = await _youTubeDownloader.DownloadAsync(
            new Uri("https://www.youtube.com/watch?v=2SVDVhzzzSY"),
            "test.%(ext)s",
            "itemId",
            "meta.txt",
            new MetadataObject('.')).ToListAsync();

        results.Count.ShouldBe(3);
        var subtitlesResult = results.Single(r => r.Path.EndsWith(".vtt"));
        var metadataResult = results.Single(r => r.Path.EndsWith(".info.json"));
        var videoResult = results.Single(r => r.Path.EndsWith(".webm"));
        videoResult.Metadata!.Contains("youtube").ShouldBeFalse();
    }
}
