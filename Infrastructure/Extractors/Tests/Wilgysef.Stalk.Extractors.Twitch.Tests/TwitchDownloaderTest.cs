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
using Wilgysef.Stalk.Core.StringFormatters;
using Wilgysef.Stalk.Extractors.TestBase;
using Wilgysef.Stalk.TestBase.Shared.Mocks;

namespace Wilgysef.Stalk.Extractors.Twitch.Tests;

public class TwitchDownloaderTest : BaseTest
{
    private static readonly string MockedDataResourcePrefix = $"{typeof(TwitchDownloaderTest).Namespace}.MockedData";

    private readonly MockProcessService _processService;
    private readonly TwitchDownloader _twitchDownloader;

    public TwitchDownloaderTest()
    {
        var interceptor = HttpClientInterceptor.Create();
        interceptor.AddForAny(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var assembly = Assembly.GetExecutingAssembly();

        var fileSystem = new MockFileSystem();
        fileSystem.AddFileFromEmbeddedResource(
            Path.Combine(Directory.GetCurrentDirectory(), "662849096#20220831_1577714683.info.json"),
            assembly,
            $"{MockedDataResourcePrefix}.Video.1577714683.info.json");

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
                    assembly.GetManifestResourceStream($"{MockedDataResourcePrefix}.YoutubeDl.1577714683.txt")!));
        });

        _twitchDownloader = new TwitchDownloader(
            fileSystem,
            new StringFormatter(),
            new FilenameSlugSelector(new[] { new WindowsFilenameSlug() }),
            new MetadataSerializer(),
            _processService,
            new HttpClient(interceptor));
    }

    [Theory]
    [InlineData("https://www.twitch.tv/videos/1577714683", true)]
    public void Can_Download(string uri, bool expected)
    {
        _twitchDownloader.CanDownload(new Uri(uri)).ShouldBe(expected);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Download_With_Subtitle_InfoJson(bool moveInfoJson)
    {
        _twitchDownloader.Config[TwitchDownloaderConfig.MoveInfoJsonToMetadataKey] = moveInfoJson;

        var results = await _twitchDownloader.DownloadAsync(
            new Uri("https://www.twitch.tv/videos/1577714683"),
            "662849096#20220831_1577714683.%(ext)s",
            "1577714683",
            "meta.txt",
            new MetadataObject()).ToListAsync();

        results.Count.ShouldBe(moveInfoJson ? 2 : 3);
        var subtitlesResult = results.Single(r => r.Path.EndsWith("662849096#20220831_1577714683.rechat.json"));
        var videoResult = results.Single(r => r.Path.EndsWith("662849096#20220831_1577714683.mp4"));

        if (moveInfoJson)
        {
            ((IDictionary<string, object>)(videoResult.Metadata!["youtube_dl"]!)).Count.ShouldBe(49);
        }
        else
        {
            var metadataResult = results.Single(r => r.Path.EndsWith("662849096#20220831_1577714683.info.json"));
            videoResult.Metadata!.Contains("youtube_dl").ShouldBeFalse();
        }
    }
}
