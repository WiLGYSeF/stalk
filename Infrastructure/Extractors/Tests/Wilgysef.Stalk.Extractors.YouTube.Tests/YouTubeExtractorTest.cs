using Shouldly;
using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Wilgysef.HttpClientInterception;
using Wilgysef.Stalk.Core.MetadataObjects;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Extractors.TestBase;
using Wilgysef.Stalk.TestBase.Shared;

namespace Wilgysef.Stalk.Extractors.YouTube.Tests;

public class YouTubeExtractorTest : BaseTest
{
    private static readonly string MockedDataResourcePrefix = $"{typeof(YouTubeExtractorTest).Namespace}.MockedData";

    private static readonly Regex BrowseRegex = new(@"^https://www.youtube.com/youtubei/v1/browse\?key=AIzaSyAO_FJ2SlqU8Q4STEHLGCilw_Y9_11qcW8", RegexOptions.Compiled);
    private static readonly Regex ThumbnailRegex = new("https://img.youtube.com/vi(?:_webp)?/(?<video>[A-Za-z0-9_-]+)/", RegexOptions.Compiled);

    private readonly YouTubeExtractor _youTubeExtractor;

    private readonly HttpClientInterceptor _httpInterceptor;

    public YouTubeExtractorTest()
    {
        _httpInterceptor = HttpClientInterceptor.Create();
        _httpInterceptor
            .AddForAny(_ => new HttpResponseMessage(HttpStatusCode.NotFound))
            .AddUri(Consts.PlaylistRegex, request =>
            {
                var query = request.RequestUri!.GetQueryParameters();
                var playlistId = query["list"];
                return HttpUtilities.GetResponseMessageFromManifestResource($"{MockedDataResourcePrefix}.Playlist.{playlistId}.html");
            })
            .AddUri(Consts.VideoRegex, request =>
            {
                var query = request.RequestUri!.GetQueryParameters();
                var videoId = query["v"];
                return HttpUtilities.GetResponseMessageFromManifestResource($"{MockedDataResourcePrefix}.Video.{videoId}.html");
            })
            .AddUri(ThumbnailRegex, request =>
            {
                var match = ThumbnailRegex.Match(request.RequestUri!.AbsoluteUri);
                var videoId = match.Groups["video"].Value;
                return HttpUtilities.GetResponseMessageFromManifestResource($"{MockedDataResourcePrefix}.Thumbnail.{videoId}.webp");
            })
            .AddUri(Consts.CommunityRegex, request =>
            {
                var match = Consts.CommunityRegex.Match(request.RequestUri!.AbsoluteUri);
                var channelId = match.Groups[Consts.CommunityRegexChannelGroup].Value;

                if (request.RequestUri.Query.Length > 0)
                {
                    var query = request.RequestUri.GetQueryParameters();
                    var postId = query["lb"];
                    return HttpUtilities.GetResponseMessageFromManifestResource($"{MockedDataResourcePrefix}.Community.{channelId}.{postId}.html");
                }

                return HttpUtilities.GetResponseMessageFromManifestResource($"{MockedDataResourcePrefix}.Community.{channelId}.html");
            })
            .AddUri(Consts.CommunityPostRegex, request =>
            {
                var match = Consts.CommunityPostRegex.Match(request.RequestUri!.AbsoluteUri);
                var postId = match.Groups[Consts.CommunityPostRegexPostGroup].Value;
                return HttpUtilities.GetResponseMessageFromManifestResource($"{MockedDataResourcePrefix}.CommunityPost.{postId}.html");
            })
            .AddUri(BrowseRegex, async (request, cancellationToken) =>
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
                    var match = Consts.CommunityRegex.Match(originalUrl.AbsoluteUri);
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

        _youTubeExtractor = new YouTubeExtractor(new HttpClient(_httpInterceptor));
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

        results.Count.ShouldBe(159);
        results.Select(r => r.Uri.AbsoluteUri).ToHashSet().Count.ShouldBe(results.Count);
        results.Select(r => r.ItemId).ToHashSet().Count.ShouldBe(results.Count);
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
        results.Select(r => r.ItemId).ToHashSet().Count.ShouldBe(results.Count);
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
        results.Select(r => r.ItemId).ToHashSet().Count.ShouldBe(results.Count);
        results.All(r => r.Type == JobTaskType.Extract).ShouldBeTrue();
    }

    [Fact]
    public async Task Get_Video()
    {
        var results = await _youTubeExtractor.ExtractAsync(
            new Uri("https://www.youtube.com/watch?v=_BSSJi-sHh8"),
            null,
            new MetadataObject('.')).ToListAsync();

        results.Count.ShouldBe(2);
        var thumbnailResult = results.Single(r => r.ItemId == "UCdYR5Oyz8Q4g0ZmB4PkTD7g#video#_BSSJi-sHh8_thumb");
        thumbnailResult.Uri.AbsoluteUri.StartsWith("https://img.youtube.com/vi_webp/_BSSJi-sHh8/maxresdefault.webp");
        thumbnailResult.Metadata!["channel.id"].ShouldBe("UCdYR5Oyz8Q4g0ZmB4PkTD7g");
        thumbnailResult.Metadata["channel.name"].ShouldBe("Uto Ch. 天使うと");
        thumbnailResult.Metadata["file.extension"].ShouldBe("webp");
        thumbnailResult.Metadata["published"].ShouldBe("20210407");
        thumbnailResult.Metadata["origin.item_id_seq"].ShouldBe("UCdYR5Oyz8Q4g0ZmB4PkTD7g#video#20210407__BSSJi-sHh8_thumb");
        thumbnailResult.Metadata["video.id"].ShouldBe("_BSSJi-sHh8");
        thumbnailResult.Metadata["video.title"].ShouldBe("Angel With A Shotgun covered by amatsukauto ໒꒱· ﾟ");
        thumbnailResult.Metadata["video.duration"].ShouldBe("03:45");
        thumbnailResult.Metadata["video.duration_seconds"].ShouldBe(225.966);
        thumbnailResult.Metadata["video.description"].ShouldBe("I love shotguns!!!\n私の初めての英語のカバー曲です。温かく見守ってください。\nIt's my first cover in English song! Please listen warmly!\n\noriginal : The Cab Angel With A Shotgun\nmix : たけまる 様 @takemaru_game\nillust : あやみ 様 @ayamy_garubinu\nvocal,movie : うと @amatsukauto\n\n☆゜+.*.+゜☆゜+.*.+゜☆゜+.*.+゜☆");
        thumbnailResult.Metadata["video.view_count"].ShouldBe("2,700,338 views");
        thumbnailResult.Metadata["video.like_count"].ShouldBe("113,285 likes");
        thumbnailResult.Metadata["video.comment_count"].ShouldBe("5.4K");
        thumbnailResult.Type.ShouldBe(JobTaskType.Download);

        var videoResult = results.Single(r => r.ItemId == "UCdYR5Oyz8Q4g0ZmB4PkTD7g#video#_BSSJi-sHh8");
        videoResult.Uri.AbsoluteUri.ShouldBe("https://www.youtube.com/watch?v=_BSSJi-sHh8");
        videoResult.Metadata!["channel.id"].ShouldBe("UCdYR5Oyz8Q4g0ZmB4PkTD7g");
        videoResult.Metadata["channel.name"].ShouldBe("Uto Ch. 天使うと");
        videoResult.Metadata["file.extension"].ShouldBe("%(ext)s");
        videoResult.Metadata["published"].ShouldBe("20210407");
        videoResult.Metadata["origin.item_id_seq"].ShouldBe("UCdYR5Oyz8Q4g0ZmB4PkTD7g#video#20210407__BSSJi-sHh8");
        videoResult.Metadata["video.id"].ShouldBe("_BSSJi-sHh8");
        videoResult.Metadata["video.title"].ShouldBe("Angel With A Shotgun covered by amatsukauto ໒꒱· ﾟ");
        videoResult.Metadata["video.duration"].ShouldBe("03:45");
        videoResult.Metadata["video.duration_seconds"].ShouldBe(225.966);
        videoResult.Metadata["video.description"].ShouldBe("I love shotguns!!!\n私の初めての英語のカバー曲です。温かく見守ってください。\nIt's my first cover in English song! Please listen warmly!\n\noriginal : The Cab Angel With A Shotgun\nmix : たけまる 様 @takemaru_game\nillust : あやみ 様 @ayamy_garubinu\nvocal,movie : うと @amatsukauto\n\n☆゜+.*.+゜☆゜+.*.+゜☆゜+.*.+゜☆");
        videoResult.Metadata["video.view_count"].ShouldBe("2,700,338 views");
        videoResult.Metadata["video.like_count"].ShouldBe("113,285 likes");
        videoResult.Metadata["video.comment_count"].ShouldBe("5.4K");
        videoResult.Type.ShouldBe(JobTaskType.Download);
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
        results.Select(r => r.ItemId).ToHashSet().Count.ShouldBe(results.Count);
        results.All(r => r.Type == JobTaskType.Download).ShouldBeTrue();
    }

    [Theory]
    [InlineData("https://www.youtube.com/channel/UCdYR5Oyz8Q4g0ZmB4PkTD7g/community?lb=UgkxNMROKyqsAjDir9C4JQHAl-96k6-x9SoP")]
    [InlineData("https://www.youtube.com/post/UgkxNMROKyqsAjDir9C4JQHAl-96k6-x9SoP")]
    public async Task Get_Community_Single(string uri)
    {
        var results = await _youTubeExtractor.ExtractAsync(
            new Uri(uri),
            null,
            new MetadataObject('.')).ToListAsync();

        results.Count.ShouldBe(38);
        results.Where(r => r.ItemId!.Contains("#community#")).Count().ShouldBe(2);

        var textResult = results.Single(r => r.ItemId == "UCdYR5Oyz8Q4g0ZmB4PkTD7g#community#UgkxNMROKyqsAjDir9C4JQHAl-96k6-x9SoP");
        textResult.Uri.AbsoluteUri.ShouldBe("data:text/plain;charset=UTF-8;base64,44K544Kx44K444Ol44O844Or44KS5b6p5rS744GV44Gb44G+44GX44Gf77yB4pypLirLmg0K44GT44KM44KS57aa44GR44Gm44GE44GP44Gu44GM55uu5qiZ44Gt44CC44CCDQrjgYLjgIHjgZ3jgYbjgYTjgYjjgbBUd2l0Y2jjgpLlp4vjgoHjgZ/jgojvvZ7vvIHvvIENCuOBn+OBvuOBq+aBr+aKnOOBjeOBq+S9v+OBhuS6iOWumuOBoOOBi+OCieaah+OBquS6uuOBr+imi+OBq+adpeOBpuOBre+9nuKZoQrjgrnjgrHjgrjjg6Xjg7zjg6vjga/ml6XmnKzmmYLplpPjgaDjgojvvZ4KCuKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqQoKSSBoYXZlIG15IHNjaGVkdWxlIGJhY2sh4pypLirLmg0KTXkgZ29hbCBpcyB0byBrZWVwIHRoaXMgZ29pbmcuDQpPaCwgYnkgdGhlIHdheSwgSSd2ZSBzdGFydGVkIFR3aXRjaC4NCiBJJ20gZ29pbmcgdG8gdXNlIGl0IHRvIHJlbGF4IG9uY2UgaW4gYSB3aGlsZSwgc28gaWYgeW91J3JlIGZyZWUsIGNvbWUgY2hlY2sgaXQgb3V0fuKZoQoK4oC7VGhpcyBzY2hlZHVsZSBpcyBpbiBKYXBhbiB0aW1lIQoKCuKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqQoKCg0KVXRv4oCZ772TIFR3aXRjaCAgaHR0cHM6Ly93d3cudHdpdGNoLnR2L3V0b19fXwoKCuKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqe+9peKcqQ==");
        textResult.Metadata!["channel.id"].ShouldBe("UCdYR5Oyz8Q4g0ZmB4PkTD7g");
        textResult.Metadata["channel.name"].ShouldBe("Uto Ch. 天使うと");
        textResult.Metadata["file.extension"].ShouldBe("txt");
        //textResult.Metadata["origin.item_id_seq"].ShouldBe("");
        textResult.Metadata["origin.uri"].ShouldBe("https://www.youtube.com/post/UgkxNMROKyqsAjDir9C4JQHAl-96k6-x9SoP");
        textResult.Metadata["post_id"].ShouldBe("UgkxNMROKyqsAjDir9C4JQHAl-96k6-x9SoP");
        //textResult.Metadata["published"].ShouldBe();
        textResult.Metadata["published_from"]!.ToString()!.StartsWith("10 months ago from ").ShouldBeTrue();
        textResult.Metadata["votes"].ShouldBe("4.3K");
        textResult.Type.ShouldBe(JobTaskType.Download);

        var imageResult = results.Single(r => r.ItemId == "UCdYR5Oyz8Q4g0ZmB4PkTD7g#community#UgkxNMROKyqsAjDir9C4JQHAl-96k6-x9SoP_image");
        imageResult.Uri.AbsoluteUri.ShouldBe("https://yt3.ggpht.com/BRWDFVKhADpFgyxc1iZgYop1k3QJGR67yoYoFulEYm35Jrvb7A2gLjpodlKVhmGtlBuUvx0VkQLD1Q=s1920-nd-v1");
        imageResult.Metadata!["channel.id"].ShouldBe("UCdYR5Oyz8Q4g0ZmB4PkTD7g");
        imageResult.Metadata["channel.name"].ShouldBe("Uto Ch. 天使うと");
        //textResult.Metadata["origin.item_id_seq"].ShouldBe("");
        imageResult.Metadata["post_id"].ShouldBe("UgkxNMROKyqsAjDir9C4JQHAl-96k6-x9SoP");
        //textResult.Metadata["published"].ShouldBe();
        imageResult.Metadata["published_from"]!.ToString()!.StartsWith("10 months ago from ").ShouldBeTrue();
        imageResult.Metadata["votes"].ShouldBe("4.3K");
        imageResult.Type.ShouldBe(JobTaskType.Download);

        var emojiResult = results.First(r => r.ItemId!.Contains("#emoji#"));
        emojiResult.ItemId.ShouldBe("UCdYR5Oyz8Q4g0ZmB4PkTD7g#emoji#YjYIYfvYAamL8gT4576oCA");
        emojiResult.Uri.AbsoluteUri.ShouldBe("https://yt3.ggpht.com/aI7NJRY3Q0B5jo-3nISoXGjmgXBNbB8ClpJaJNP5IhTLbGNWDea_m_XbTx5cIU5GKmZwEMKQoA=w512-h512-c-k-nd");
        emojiResult.Metadata!["file.extension"].ShouldBe("png");
        emojiResult.Metadata["emoji_name"].ShouldBe("konuto");
        emojiResult.Type.ShouldBe(JobTaskType.Download);
    }

    [Theory]
    [InlineData("https://www.youtube.com/channel/UCdYR5Oyz8Q4g0ZmB4PkTD7g/community")]
    [InlineData("https://www.youtube.com/channel/UCdYR5Oyz8Q4g0ZmB4PkTD7g/community?lb=UgkxNMROKyqsAjDir9C4JQHAl-96k6-x9SoP")]
    [InlineData("https://www.youtube.com/post/UgkxNMROKyqsAjDir9C4JQHAl-96k6-x9SoP")]
    public async Task Get_Community_EmojisOnly(string uri)
    {
        _youTubeExtractor.Config[YouTubeExtractorConfig.CommunityEmojisOnlyKey] = true;

        var results = await _youTubeExtractor.ExtractAsync(
            new Uri(uri),
            null,
            new MetadataObject('.')).ToListAsync();

        results.Count.ShouldBe(36);
        results.All(r => r.ItemId!.Contains("#emoji#")).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Config_Cookies(bool multipleCookies)
    {
        HttpRequestMessage? request = null;

        _httpInterceptor.RequestProcessed += HttpInterceptor_RequestProcessed;

        var cookies = new Dictionary<string, string>();
        if (multipleCookies)
        {
            cookies["YSC"] = "_wwCnBu9guc";
            cookies["VISITOR_INFO1_LIVE"] = "MHllVv46iSU";
            _youTubeExtractor.Config[YouTubeExtractorConfig.CookiesKey] = cookies.Select(p => $"{p.Key}={p.Value}");
        }
        else
        {
            cookies["VISITOR_INFO1_LIVE"] = "MHllVv46iSU";
            _youTubeExtractor.Config[YouTubeExtractorConfig.CookiesKey] = cookies.Select(p => $"{p.Key}={p.Value}").Single();
        }

        var results = await _youTubeExtractor.ExtractAsync(
            new Uri("https://www.youtube.com/watch?v=_BSSJi-sHh8"),
            null,
            new MetadataObject('.')).ToListAsync();

        request!.Headers.Single(p => p.Key == "Cookie").Value
            .ShouldBe(new[] { string.Join("; ", cookies.Select(q => $"{q.Key}={q.Value}")) });

        void HttpInterceptor_RequestProcessed(object? sender, HttpRequestMessage e)
        {
            request = e;
        }
    }

    [Fact]
    public async Task Config_NoWebpThumbnail()
    {
        _youTubeExtractor.Config[YouTubeExtractorConfig.UseWebpThumbnailsKey] = false;

        var results = await _youTubeExtractor.ExtractAsync(
            new Uri("https://www.youtube.com/watch?v=_BSSJi-sHh8"),
            null,
            new MetadataObject('.')).ToListAsync();
        var thumbnailResult = results.Single(r => r.ItemId == "UCdYR5Oyz8Q4g0ZmB4PkTD7g#video#_BSSJi-sHh8_thumb");
        thumbnailResult.Uri.AbsoluteUri.EndsWith(".jpg").ShouldBeTrue();
    }
}
