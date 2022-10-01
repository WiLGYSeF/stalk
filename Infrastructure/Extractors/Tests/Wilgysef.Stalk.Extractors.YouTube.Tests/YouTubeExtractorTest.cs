using Shouldly;
using System.Net;
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

    private const string UriPrefixRegex = @"^(?:https?://)?(?:(?:www\.|m\.)?youtube\.com|youtu\.be)";
    private static readonly Regex PlaylistRegex = new(UriPrefixRegex + @"/playlist\?", RegexOptions.Compiled);
    private static readonly Regex BrowseRegex = new(@"^https://www.youtube.com/youtubei/v1/browse\?key=AIzaSyAO_FJ2SlqU8Q4STEHLGCilw_Y9_11qcW8", RegexOptions.Compiled);
    private static readonly Regex VideoRegex = new(UriPrefixRegex + @"/watch\?", RegexOptions.Compiled);
    private static readonly Regex CommunityRegex = new(UriPrefixRegex + @"/c(?:hannel)?/(?<channel>[A-Za-z0-9_-]+)/community", RegexOptions.Compiled);

    private readonly YouTubeExtractor _youTubeExtractor;

    public YouTubeExtractorTest()
    {
        var messageHandler = new MockHttpMessageHandler();
        messageHandler
            .AddEndpoint(PlaylistRegex, request =>
            {
                var query = request.RequestUri!.GetQueryParameters();
                var playlistId = query["list"];
                return HttpUtilities.GetResponseMessageFromManifestResource($"{MockedDataResourcePrefix}.Playlist.{playlistId}.html");
            })
            .AddEndpoint(VideoRegex, request =>
            {
                var query = request.RequestUri!.GetQueryParameters();
                var videoId = query["v"];
                return HttpUtilities.GetResponseMessageFromManifestResource($"{MockedDataResourcePrefix}.Video.{videoId}.html");
            })
            .AddEndpoint(CommunityRegex, request =>
            {
                var match = CommunityRegex.Match(request.RequestUri!.AbsoluteUri);
                var channelId = match.Groups["channel"].Value;

                if (request.RequestUri.Query.Length > 0)
                {
                    var query = request.RequestUri.GetQueryParameters();
                    var postId = query["lb"];
                    return HttpUtilities.GetResponseMessageFromManifestResource($"{MockedDataResourcePrefix}.Community.{channelId}.{postId}.html");
                }

                return HttpUtilities.GetResponseMessageFromManifestResource($"{MockedDataResourcePrefix}.Community.{channelId}.html");
            })
            .AddEndpoint(BrowseRegex, async (request, cancellationToken) =>
            {
                var json = JsonSerializer.Deserialize<JsonNode>(await request.Content!.ReadAsStringAsync(cancellationToken));
                var originalUrl = new Uri(json["context"]["client"]["originalUrl"].ToString());

                if (originalUrl.AbsolutePath.Contains("playlist"))
                {
                    var query = originalUrl.GetQueryParameters();
                    var playlistId = query["list"];
                    var continuationToken = json["continuation"].ToString();
                    var continuationHash = MD5.HashData(Encoding.UTF8.GetBytes(continuationToken)).ToHexString();
                    return HttpUtilities.GetResponseMessageFromManifestResource($"{MockedDataResourcePrefix}.Playlist.{playlistId}.{continuationHash}.json");
                }
                else if (originalUrl.AbsolutePath.Contains("community"))
                {
                    var match = CommunityRegex.Match(originalUrl.AbsoluteUri);
                    var channelId = match.Groups["channel"].Value;
                    var continuationToken = json["continuation"].ToString();
                    var continuationHash = MD5.HashData(Encoding.UTF8.GetBytes(continuationToken)).ToHexString();
                    return HttpUtilities.GetResponseMessageFromManifestResource($"{MockedDataResourcePrefix}.Community.{channelId}.{continuationHash}.json");
                }
                else
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }
            });

        _youTubeExtractor = new YouTubeExtractor(new HttpClient(messageHandler));
    }

    [Theory]
    [InlineData("https://www.youtube.com/channel/UCdYR5Oyz8Q4g0ZmB4PkTD7g", true)]
    [InlineData("https://www.youtube.com/channel/UCdYR5Oyz8Q4g0ZmB4PkTD7g/", true)]
    [InlineData("https://www.youtube.com/channel/UCdYR5Oyz8Q4g0ZmB4PkTD7g/videos", true)]
    [InlineData("https://www.youtube.com/playlist?list=UUdYR5Oyz8Q4g0ZmB4PkTD7g", true)]
    [InlineData("https://www.youtube.com/watch?v=_BSSJi-sHh8", true)]
    [InlineData("https://www.youtube.com/channel/UCdYR5Oyz8Q4g0ZmB4PkTD7g/community", true)]
    public void Can_Extract(string uri, bool expected)
    {
        _youTubeExtractor.CanExtract(new Uri(uri)).ShouldBe(expected);
    }

    [Fact]
    public async Task Get_Channel()
    {
        var results = await _youTubeExtractor.ExtractAsync(
            new Uri("https://www.youtube.com/channel/UCdYR5Oyz8Q4g0ZmB4PkTD7g"),
            null,
            new MetadataObject('.')).ToListAsync();

        results.Count.ShouldBe(136);
        results.Select(r => r.Uri.AbsoluteUri).ToHashSet().Count.ShouldBe(results.Count);
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

    [Fact]
    public async Task Get_Playlist()
    {
        var results = await _youTubeExtractor.ExtractAsync(
            new Uri("https://www.youtube.com/playlist?list=UUdYR5Oyz8Q4g0ZmB4PkTD7g"),
            null,
            new MetadataObject('.')).ToListAsync();

        results.Count.ShouldBe(130);
        results.Select(r => r.Uri.AbsoluteUri).ToHashSet().Count.ShouldBe(results.Count);
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
    public async Task Get_Community()
    {
        var results = await _youTubeExtractor.ExtractAsync(
            new Uri("https://www.youtube.com/channel/UCdYR5Oyz8Q4g0ZmB4PkTD7g/community"),
            null,
            new MetadataObject('.')).ToListAsync();

        results.Count.ShouldBe(6);
        results.Select(r => r.Uri.AbsoluteUri).ToHashSet().Count.ShouldBe(results.Count);
    }

    [Fact]
    public async Task Get_Community_Single()
    {
        var results = await _youTubeExtractor.ExtractAsync(
            new Uri("https://www.youtube.com/channel/UCdYR5Oyz8Q4g0ZmB4PkTD7g/community?lb=UgkxNMROKyqsAjDir9C4JQHAl-96k6-x9SoP"),
            null,
            new MetadataObject('.')).ToListAsync();

        results.Count.ShouldBe(38);
        results.Where(r => r.ItemId.Contains("#community#")).Count().ShouldBe(2);

        var textResult = results.Single(r => r.ItemId == "UCdYR5Oyz8Q4g0ZmB4PkTD7g#community#UgkxNMROKyqsAjDir9C4JQHAl-96k6-x9SoP");
        textResult.Uri.AbsoluteUri.ShouldBe("data:;base64,44K544Kx44K444Ol44O844Or44KS5b6p5rS744GV44Gb44G+44GX44Gf77yB4pypLirLmg0K44GT44KM44KS57aa44GR44Gm44GE44GP44Gu44GM55uu5qiZ44Gt44CC44CCDQrjgYLjgIHjgZ3jgYbjgYTjgYjjgbBUd2l0Y2jjgpLlp4vjgoHjgZ/jgojvvZ7vvIHvvIENCuOBn+OBvuOBq+aBr+aKnOOBjeOBq+S9v+OBhuS6iOWumuOBoOOBi+OCieaah+OBquS6uuOBr+imi+OBq+adpeOBpuOBre+9nuKZoQrjgrnjgrHjgrjjg6Xjg7zjg6vjga/ml6XmnKzmmYLplpPjgaDjgojvvZ4KCuKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqQoKSSBoYXZlIG15IHNjaGVkdWxlIGJhY2sh4pypLirLmg0KTXkgZ29hbCBpcyB0byBrZWVwIHRoaXMgZ29pbmcuDQpPaCwgYnkgdGhlIHdheSwgSSd2ZSBzdGFydGVkIFR3aXRjaC4NCiBJJ20gZ29pbmcgdG8gdXNlIGl0IHRvIHJlbGF4IG9uY2UgaW4gYSB3aGlsZSwgc28gaWYgeW91J3JlIGZyZWUsIGNvbWUgY2hlY2sgaXQgb3V0fuKZoQoK4oC7VGhpcyBzY2hlZHVsZSBpcyBpbiBKYXBhbiB0aW1lIQoKCuKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqQoKCg0KVXRv4oCZ772TIFR3aXRjaCAgaHR0cHM6Ly93d3cudHdpdGNoLnR2L3V0b19fXwoKCuKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqQ==");
        textResult.Metadata!["channel.id"].ShouldBe("UCdYR5Oyz8Q4g0ZmB4PkTD7g");
        textResult.Metadata["channel.name"].ShouldBe("Uto Ch. 天使うと");
        textResult.Metadata["file.extension"].ShouldBe("txt");
        //textResult.Metadata["origin.item_id_seq"].ShouldBe("");
        textResult.Metadata["origin.uri"].ShouldBe("https://www.youtube.com/channel/UCdYR5Oyz8Q4g0ZmB4PkTD7g/community?lb=UgkxNMROKyqsAjDir9C4JQHAl-96k6-x9SoP");
        textResult.Metadata["post_id"].ShouldBe("UgkxNMROKyqsAjDir9C4JQHAl-96k6-x9SoP");
        //textResult.Metadata["published"].ShouldBe();
        textResult.Metadata["published_from"]!.ToString()!.StartsWith("10 months ago from ").ShouldBeTrue();
        textResult.Metadata["votes"].ShouldBe("4.3K");
        textResult.Type.ShouldBe(JobTaskType.Download);

        var imageResult = results.Single(r => r.ItemId == "UCdYR5Oyz8Q4g0ZmB4PkTD7g#community#UgkxNMROKyqsAjDir9C4JQHAl-96k6-x9SoP-image");
        imageResult.Uri.AbsoluteUri.ShouldBe("https://yt3.ggpht.com/BRWDFVKhADpFgyxc1iZgYop1k3QJGR67yoYoFulEYm35Jrvb7A2gLjpodlKVhmGtlBuUvx0VkQLD1Q=s1920-nd-v1");
        imageResult.Metadata!["channel.id"].ShouldBe("UCdYR5Oyz8Q4g0ZmB4PkTD7g");
        imageResult.Metadata["channel.name"].ShouldBe("Uto Ch. 天使うと");
        //textResult.Metadata["origin.item_id_seq"].ShouldBe("");
        imageResult.Metadata["post_id"].ShouldBe("UgkxNMROKyqsAjDir9C4JQHAl-96k6-x9SoP");
        //textResult.Metadata["published"].ShouldBe();
        imageResult.Metadata["published_from"]!.ToString()!.StartsWith("10 months ago from ").ShouldBeTrue();
        imageResult.Metadata["votes"].ShouldBe("4.3K");
        imageResult.Type.ShouldBe(JobTaskType.Download);

        var emojiResult = results.First(r => r.ItemId.Contains("#emoji#"));
        emojiResult.ItemId.ShouldBe("UCdYR5Oyz8Q4g0ZmB4PkTD7g#emoji#YjYIYfvYAamL8gT4576oCA");
        emojiResult.Uri.AbsoluteUri.ShouldBe("https://yt3.ggpht.com/aI7NJRY3Q0B5jo-3nISoXGjmgXBNbB8ClpJaJNP5IhTLbGNWDea_m_XbTx5cIU5GKmZwEMKQoA=w512-h512-c-k-nd");
        emojiResult.Metadata!["file.extension"].ShouldBe("png");
        emojiResult.Metadata["emoji_name"].ShouldBe("konuto");
        emojiResult.Type.ShouldBe(JobTaskType.Download);
    }
}
