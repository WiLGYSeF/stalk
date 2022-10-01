using Shouldly;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Wilgysef.Stalk.Core.MetadataObjects;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Extractors.TestBase;
using Wilgysef.Stalk.TestBase.Shared;
using Wilgysef.Stalk.TestBase.Shared.Mocks;

namespace Wilgysef.Stalk.Extractors.YouTube.Tests;

public class YouTubeExtractorTest : BaseTest
{
    private static readonly string MockedDataResourcePrefix = $"{typeof(YouTubeExtractorTest).Namespace}.MockedData";

    private const string UriPrefixRegex = @"^(?:https?://)?(?:(?:www\.|m\.)?youtube.com|youtu\.be)";
    private static readonly Regex ChannelRegex = new(UriPrefixRegex + @"/(?<segment>c(?:hannel)?)/(?<channel>[A-Za-z0-9_-]+)", RegexOptions.Compiled);
    private static readonly Regex VideosRegex = new(UriPrefixRegex + @"/c(?:hannel)?/(?<channel>[A-Za-z0-9_-]+)/videos", RegexOptions.Compiled);
    private static readonly Regex PlaylistRegex = new(UriPrefixRegex + @"/playlist\?", RegexOptions.Compiled);
    private static readonly Regex PlaylistNextRegex = new(@"^https://www.youtube.com/youtubei/v1/browse\?key=AIzaSyAO_FJ2SlqU8Q4STEHLGCilw_Y9_11qcW8", RegexOptions.Compiled);
    private static readonly Regex VideoRegex = new(UriPrefixRegex + @"/watch\?", RegexOptions.Compiled);
    private static readonly Regex CommunityRegex = new(UriPrefixRegex + @"/c(?:hannel)?/(?<channel>[A-Za-z0-9_-]+)/community", RegexOptions.Compiled);

    private readonly YouTubeExtractor _youTubeExtractor;

    public YouTubeExtractorTest()
    {
        var messageHandler = new MockHttpMessageHandler();
        messageHandler
            .AddEndpoint(VideoRegex, request =>
            {
                var query = request.RequestUri!.GetQueryParameters();
                var videoId = query["v"];
                return HttpUtilities.GetResponseMessageFromManifestResource($"{MockedDataResourcePrefix}.Video.{videoId}.html");
            })
            .AddEndpoint(PlaylistRegex, request =>
            {
                var query = request.RequestUri!.GetQueryParameters();
                var playlistId = query["list"];
                return HttpUtilities.GetResponseMessageFromManifestResource($"{MockedDataResourcePrefix}.Playlist.{playlistId}.html");
            })
            .AddEndpoint(PlaylistNextRegex, async (request, cancellationToken) =>
            {
                var json = JsonSerializer.Deserialize<JsonNode>(await request.Content!.ReadAsStringAsync(cancellationToken));
                var playlistUri = new Uri(json["context"]["client"]["originalUrl"].ToString());
                var query = playlistUri.GetQueryParameters();
                var playlistId = query["list"];
                var continuationToken = json["continuation"].ToString();
                var continuationHash = MD5.HashData(Encoding.UTF8.GetBytes(continuationToken)).ToHexString();
                return HttpUtilities.GetResponseMessageFromManifestResource($"{MockedDataResourcePrefix}.Playlist.{playlistId}.{continuationHash}.json");
            });

        _youTubeExtractor = new YouTubeExtractor(new HttpClient(messageHandler));
    }

    [Fact]
    public async Task Get_Video()
    {
        var results = await _youTubeExtractor.ExtractAsync(
            new Uri("https://www.youtube.com/watch?v=_BSSJi-sHh8"),
            null,
            new MetadataObject('.')).ToListAsync();

        results.Count.ShouldBe(1);
        var video = results.Single();
        video.Uri.AbsoluteUri.ShouldBe("https://www.youtube.com/watch?v=_BSSJi-sHh8");
        video.ItemId.ShouldBe("UCdYR5Oyz8Q4g0ZmB4PkTD7g#video#_BSSJi-sHh8");
        video.Type.ShouldBe(JobTaskType.Download);
    }

    [Fact]
    public async Task Get_Videos()
    {
        var results = await _youTubeExtractor.ExtractAsync(
            new Uri("https://www.youtube.com/channel/UCdYR5Oyz8Q4g0ZmB4PkTD7g/videos"),
            null,
            new MetadataObject('.')).ToListAsync();

        results.Count.ShouldBe(130);
        results.Select(r => r.Uri.AbsoluteUri).ToHashSet().Count.ShouldBe(results.Count);
    }
}
