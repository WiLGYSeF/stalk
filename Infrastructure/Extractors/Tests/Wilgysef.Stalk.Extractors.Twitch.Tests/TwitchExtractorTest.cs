using Shouldly;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Wilgysef.HttpClientInterception;
using Wilgysef.Stalk.Core.MetadataObjects;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;
using Wilgysef.Stalk.Core.Utilities;
using Wilgysef.Stalk.Extractors.TestBase;
using Wilgysef.Stalk.TestBase.Shared;

namespace Wilgysef.Stalk.Extractors.Twitch.Tests;

public class TwitchExtractorTest
{
    private static readonly string MockedDataResourcePrefix = $"{typeof(TwitchExtractorTest).Namespace}.MockedData";

    private readonly TwitchExtractor _twitchExtractor;

    private readonly HttpClientInterceptor _httpInterceptor;

    public TwitchExtractorTest()
    {
        _httpInterceptor = HttpClientInterceptor.Create();
        _httpInterceptor
            .AddForAny(_ => new HttpResponseMessage(HttpStatusCode.NotFound))
            .AddUri(Consts.GraphQlUri, request =>
            {
                var json = JsonSerializer.Deserialize<List<JsonElement>>(request.Content!.ReadAsStream());
                var qlRequest = (Dictionary<string, object>)JsonUtils.GetJsonElementValue(json.Single(), out _)!;
                var operation = qlRequest["operationName"].ToString();
                var variables = (Dictionary<string, object>)qlRequest["variables"];

                if (operation == "FilterableVideoTower_Videos")
                {
                    var username = variables["channelOwnerLogin"].ToString();

                    if (variables.TryGetValue("cursor", out var cursor))
                    {
                        var cursorHash = MD5.HashData(Encoding.UTF8.GetBytes(cursor.ToString()!)).ToHexString();
                        return HttpUtilities.GetResponseMessageFromManifestResource($"{MockedDataResourcePrefix}.{operation}.{username}.{cursorHash}.json");
                    }
                    else
                    {
                        return HttpUtilities.GetResponseMessageFromManifestResource($"{MockedDataResourcePrefix}.{operation}.{username}.json");
                    }
                }
                else if (operation == "ChannelVideoCore")
                {
                    var videoId = variables["videoID"].ToString();
                    return HttpUtilities.GetResponseMessageFromManifestResource($"{MockedDataResourcePrefix}.{operation}.{videoId}.json");
                }
                else if (operation == "VideoMetadata")
                {
                    var username = variables["channelLogin"].ToString();
                    var videoId = variables["videoID"].ToString();
                    return HttpUtilities.GetResponseMessageFromManifestResource($"{MockedDataResourcePrefix}.{operation}.{username}.{videoId}.json");
                }
                else
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }
            });

        _twitchExtractor = new(new HttpClient(_httpInterceptor));
    }

    [Theory]
    [InlineData("https://www.twitch.tv/utonyan", true)]
    [InlineData("https://twitch.tv/utonyan", true)]
    [InlineData("https://www.twitch.tv/utonyan/", true)]
    [InlineData("https://www.twitch.tv/utonyan/videos", true)]
    [InlineData("https://www.twitch.tv/utonyan/videos/", true)]
    [InlineData("https://www.twitch.tv/videos/123456789", true)]
    [InlineData("https://www.twitch.tv/utonyan/clips", true)]
    [InlineData("https://www.twitch.tv/utonyan/clips/", true)]
    [InlineData("https://www.twitch.tv/utonyan/clip/SilkyTrappedLousePanicBasket-rIzy-DnZdAQTfq_V", true)]
    public void Can_Extract(string uri, bool expected)
    {
        _twitchExtractor.CanExtract(new Uri(uri)).ShouldBe(expected);
    }

    [Theory]
    [InlineData("https://www.twitch.tv/utonyan", null)]
    [InlineData("https://www.twitch.tv/utonyan/videos", null)]
    [InlineData("https://www.twitch.tv/videos/123456789", "123456789")]
    [InlineData("https://www.twitch.tv/utonyan/clips", null)]
    [InlineData("https://www.twitch.tv/utonyan/clip/SilkyTrappedLousePanicBasket-rIzy-DnZdAQTfq_V", "SilkyTrappedLousePanicBasket-rIzy-DnZdAQTfq_V")]
    public void GetItemId(string uri, string? itemId)
    {
        _twitchExtractor.GetItemId(new Uri(uri)).ShouldBe(itemId);
    }

    [Fact]
    public async Task Get_Videos()
    {
        var results = await _twitchExtractor.ExtractAsync(
            new Uri("https://www.twitch.tv/utonyan"),
            null,
            new MetadataObject('.')).ToListAsync();

        results.Count.ShouldBe(22);
        results.Select(r => r.Uri).ToHashSet().Count.ShouldBe(results.Count);
        results.Select(r => r.ItemId).ToHashSet().Count.ShouldBe(results.Count);
    }

    [Fact]
    public async Task Get_Video()
    {
        var results = await _twitchExtractor.ExtractAsync(
            new Uri("https://www.twitch.tv/videos/1586110158"),
            null,
            new MetadataObject('.')).ToListAsync();

        results.Count.ShouldBe(2);
        var thumbnailResult = results.Single(r => r.ItemId == "1586110158#thumb");
        thumbnailResult.Uri.ShouldBe("https://static-cdn.jtvnw.net/cf_vods/d3vd9lfkzbru3h/a2197cb3f8db6cc072b2_utonyan_39655744983_1662723806//thumb/thumb0-90x60.jpg");
        thumbnailResult.Metadata!["file.extension"].ShouldBe("jpg");
        thumbnailResult.Metadata["origin.item_id_seq"].ShouldBe("662849096#20220909_1586110158#thumb");
        thumbnailResult.Metadata["user.id"].ShouldBe("662849096");
        thumbnailResult.Metadata["user.login"].ShouldBe("utonyan");
        thumbnailResult.Metadata["user.name"].ShouldBe("utonyan");
        thumbnailResult.Metadata["video.id"].ShouldBe("1586110158");
        thumbnailResult.Metadata["video.length_seconds"].ShouldBe(9178);
        thumbnailResult.Metadata["video.length"].ShouldBe("02:32:58");
        thumbnailResult.Metadata["video.title"].ShouldBe("Twitch partner get!!!!!!!!!");
        thumbnailResult.Metadata["video.view_count"].ShouldBe(19762);
        thumbnailResult.Metadata["video.game.id"].ShouldBe("518006");
        thumbnailResult.Metadata["video.game.name"].ShouldBe("Stray");
        thumbnailResult.Metadata["video.game.boxart_url"].ShouldBe("https://static-cdn.jtvnw.net/ttv-boxart/518006_IGDB-{width}x{height}.jpg");
        thumbnailResult.Type.ShouldBe(JobTaskType.Download);

        var videoResult = results.Single(r => r.ItemId == "1586110158");
        videoResult.Uri.ShouldBe("https://www.twitch.tv/videos/1586110158");
        videoResult.Metadata!["file.extension"].ShouldBe("%(ext)s");
        videoResult.Metadata["origin.item_id_seq"].ShouldBe("662849096#20220909_1586110158");
        videoResult.Metadata["user.id"].ShouldBe("662849096");
        videoResult.Metadata["user.login"].ShouldBe("utonyan");
        videoResult.Metadata["user.name"].ShouldBe("utonyan");
        videoResult.Metadata["video.id"].ShouldBe("1586110158");
        videoResult.Metadata["video.length_seconds"].ShouldBe(9178);
        videoResult.Metadata["video.length"].ShouldBe("02:32:58");
        videoResult.Metadata["video.title"].ShouldBe("Twitch partner get!!!!!!!!!");
        videoResult.Metadata["video.view_count"].ShouldBe(19762);
        videoResult.Metadata["video.game.id"].ShouldBe("518006");
        videoResult.Metadata["video.game.name"].ShouldBe("Stray");
        videoResult.Metadata["video.game.boxart_url"].ShouldBe("https://static-cdn.jtvnw.net/ttv-boxart/518006_IGDB-{width}x{height}.jpg");
        videoResult.Type.ShouldBe(JobTaskType.Download);
    }
}