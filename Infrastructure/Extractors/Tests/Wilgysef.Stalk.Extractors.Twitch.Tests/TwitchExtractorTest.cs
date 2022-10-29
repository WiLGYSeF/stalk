using Shouldly;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Wilgysef.HttpClientInterception;
using Wilgysef.Stalk.Core.MetadataObjects;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Extensions;
using Wilgysef.Stalk.Core.Utilities;

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
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(json!.Select(e => GetGraphQlResponse(e)).ToList()),
                };
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
    [InlineData("https://clips.twitch.tv/ResoluteKathishOcelotArgieB8-_aFeNcWSMiNC34Bc", true)]
    [InlineData("https://www.twitch.tv/utonyan/about", true)]
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
    [InlineData("https://clips.twitch.tv/ResoluteKathishOcelotArgieB8-_aFeNcWSMiNC34Bc", "ResoluteKathishOcelotArgieB8-_aFeNcWSMiNC34Bc")]
    public void GetItemId(string uri, string? itemId)
    {
        _twitchExtractor.GetItemId(new Uri(uri)).ShouldBe(itemId);
    }

    [Fact]
    public async Task Get_Channel()
    {
        var results = await _twitchExtractor.ExtractAsync(
            new Uri("https://www.twitch.tv/utonyan"),
            null,
            new MetadataObject()).ToListAsync();

        results.Count.ShouldBe(3);
        var videosResult = results.Single(r => r.Uri.AbsoluteUri == "https://www.twitch.tv/utonyan/videos");
        var clipsResult = results.Single(r => r.Uri.AbsoluteUri == "https://www.twitch.tv/utonyan/clips");
        var aboutResult = results.Single(r => r.Uri.AbsoluteUri == "https://www.twitch.tv/utonyan/about");
    }

    [Fact]
    public async Task Get_Videos()
    {
        var results = await _twitchExtractor.ExtractAsync(
            new Uri("https://www.twitch.tv/utonyan/videos"),
            null,
            new MetadataObject()).ToListAsync();

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
            new MetadataObject()).ToListAsync();

        results.Count.ShouldBe(2);
        var thumbnailResult = results.Single(r => r.ItemId == "1586110158#thumb");
        thumbnailResult.Uri.AbsoluteUri.ShouldBe("https://static-cdn.jtvnw.net/cf_vods/d3vd9lfkzbru3h/a2197cb3f8db6cc072b2_utonyan_39655744983_1662723806//thumb/thumb0-90x60.jpg");
        thumbnailResult.Metadata!["channel", "id"].ShouldBe("662849096");
        thumbnailResult.Metadata["channel", "login"].ShouldBe("utonyan");
        thumbnailResult.Metadata["channel", "name"].ShouldBe("utonyan");
        thumbnailResult.Metadata["file", "extension"].ShouldBe("jpg");
        thumbnailResult.Metadata["origin", "item_id_seq"].ShouldBe("662849096#video#20220909_1586110158#thumb");
        thumbnailResult.Metadata["video", "id"].ShouldBe("1586110158");
        thumbnailResult.Metadata["video", "length_seconds"].ShouldBe(9178);
        thumbnailResult.Metadata["video", "length"].ShouldBe("02:32:58");
        thumbnailResult.Metadata["video", "published_at"].ShouldBe("20220909");
        thumbnailResult.Metadata["video", "title"].ShouldBe("Twitch partner get!!!!!!!!!");
        thumbnailResult.Metadata["video", "view_count"].ShouldBe(19762);
        thumbnailResult.Metadata["video", "game", "id"].ShouldBe("518006");
        thumbnailResult.Metadata["video", "game", "name"].ShouldBe("Stray");
        thumbnailResult.Metadata["video", "game", "boxart_url"].ShouldBe("https://static-cdn.jtvnw.net/ttv-boxart/518006_IGDB-{width}x{height}.jpg");
        thumbnailResult.Type.ShouldBe(JobTaskType.Download);

        var videoResult = results.Single(r => r.ItemId == "1586110158");
        videoResult.Uri.AbsoluteUri.ShouldBe("https://www.twitch.tv/videos/1586110158");
        videoResult.Metadata!["channel", "id"].ShouldBe("662849096");
        videoResult.Metadata["channel", "login"].ShouldBe("utonyan");
        videoResult.Metadata["channel", "name"].ShouldBe("utonyan");
        videoResult.Metadata["file", "extension"].ShouldBe("%(ext)s");
        videoResult.Metadata["origin", "item_id_seq"].ShouldBe("662849096#video#20220909_1586110158");
        videoResult.Metadata["video", "id"].ShouldBe("1586110158");
        videoResult.Metadata["video", "length_seconds"].ShouldBe(9178);
        videoResult.Metadata["video", "length"].ShouldBe("02:32:58");
        videoResult.Metadata["video", "published_at"].ShouldBe("20220909");
        videoResult.Metadata["video", "title"].ShouldBe("Twitch partner get!!!!!!!!!");
        videoResult.Metadata["video", "view_count"].ShouldBe(19762);
        videoResult.Metadata["video", "game", "id"].ShouldBe("518006");
        videoResult.Metadata["video", "game", "name"].ShouldBe("Stray");
        videoResult.Metadata["video", "game", "boxart_url"].ShouldBe("https://static-cdn.jtvnw.net/ttv-boxart/518006_IGDB-{width}x{height}.jpg");
        videoResult.Type.ShouldBe(JobTaskType.Download);
    }

    [Fact]
    public async Task Get_Clips()
    {
        var results = await _twitchExtractor.ExtractAsync(
            new Uri("https://www.twitch.tv/utonyan/clips"),
            null,
            new MetadataObject()).ToListAsync();

        results.Count.ShouldBe(40);
        results.Select(r => r.Uri).ToHashSet().Count.ShouldBe(results.Count);
        results.Select(r => r.ItemId).ToHashSet().Count.ShouldBe(results.Count);
        results.All(r => r.Type == JobTaskType.Extract).ShouldBeTrue();
    }

    [Theory]
    [InlineData("https://www.twitch.tv/utonyan/clip/ResoluteKathishOcelotArgieB8-_aFeNcWSMiNC34Bc")]
    [InlineData("https://clips.twitch.tv/ResoluteKathishOcelotArgieB8-_aFeNcWSMiNC34Bc")]
    public async Task Get_Clip(string uri)
    {
        var results = await _twitchExtractor.ExtractAsync(
            new Uri(uri),
            null,
            new MetadataObject()).ToListAsync();

        var result = results.Single();
        result.ItemId.ShouldBe("ResoluteKathishOcelotArgieB8-_aFeNcWSMiNC34Bc");
        result.Uri.AbsoluteUri.ShouldBe("https://production.assets.clips.twitchcdn.net/9m6CDf2hXjXFkjIjQt-AXA/AT-cm%7C9m6CDf2hXjXFkjIjQt-AXA.mp4?sig=f516d2c7571b32c122cd8baab8bb69520827a565&token=%7b%22authorization%22%3a%7b%22forbidden%22%3afalse%2c%22reason%22%3a%22%22%7d%2c%22clip_uri%22%3a%22https%3a%2f%2fproduction.assets.clips.twitchcdn.net%2f9m6CDf2hXjXFkjIjQt-AXA%2fAT-cm%257C9m6CDf2hXjXFkjIjQt-AXA.mp4%22%2c%22device_id%22%3a%22oopGMf8bQJZyCGecMGjPM8M2zaPhdHBS%22%2c%22expires%22%3a1665866268%2c%22user_id%22%3a%22%22%2c%22version%22%3a2%7d");
        result.Metadata!["channel", "id"].ShouldBe("662849096");
        result.Metadata["channel", "login"].ShouldBe("utonyan");
        result.Metadata["channel", "name"].ShouldBe("utonyan");
        result.Metadata["clip", "created_at"].ShouldBe("20220728");
        result.Metadata["clip", "curator", "id"].ShouldBe("442539197");
        result.Metadata["clip", "curator", "login"].ShouldBe("renanamiya");
        result.Metadata["clip", "curator", "name"].ShouldBe("renanamiya");
        result.Metadata["clip", "duration"].ShouldBe("00:49");
        result.Metadata["clip", "duration_seconds"].ShouldBe(49);
        result.Metadata["clip", "game", "id"].ShouldBe("518006");
        result.Metadata["clip", "game", "name"].ShouldBe("Stray");
        result.Metadata["clip", "id"].ShouldBe("1796802747");
        result.Metadata["clip", "slug"].ShouldBe("ResoluteKathishOcelotArgieB8-_aFeNcWSMiNC34Bc");
        result.Metadata["clip", "title"].ShouldBe("Uto vibing");
        result.Metadata["clip", "url"].ShouldBe("https://clips.twitch.tv/ResoluteKathishOcelotArgieB8-_aFeNcWSMiNC34Bc");
        result.Metadata["clip", "view_count"].ShouldBe(1691);
        result.Metadata["file", "extension"].ShouldBe("mp4");
        result.Metadata["origin", "item_id_seq"].ShouldBe("662849096#clip#20220728_ResoluteKathishOcelotArgieB8-_aFeNcWSMiNC34Bc");
        result.Metadata["origin", "uri"].ShouldBe("https://clips.twitch.tv/ResoluteKathishOcelotArgieB8-_aFeNcWSMiNC34Bc");
        result.Type.ShouldBe(JobTaskType.Download);
    }

    [Fact]
    public async Task Get_About()
    {
        var results = await _twitchExtractor.ExtractAsync(
            new Uri("https://www.twitch.tv/utonyan/about"),
            null,
            new MetadataObject()).ToListAsync();

        results.Count.ShouldBe(18);
        results.Select(r => r.Uri).ToHashSet().Count.ShouldBe(results.Count);
        results.Select(r => r.ItemId).ToHashSet().Count.ShouldBe(results.Count);
        results.All(r => r.Type == JobTaskType.Download).ShouldBeTrue();

        var gifResult = results.Single(r => r.ItemId == "emotesv2_9e8516dacaa44f6181451f6e99666a9e");
        gifResult.Uri.AbsoluteUri.ShouldBe("https://static-cdn.jtvnw.net/emoticons/v2/emotesv2_9e8516dacaa44f6181451f6e99666a9e/default/dark/1.0");
        gifResult.Metadata!["emote", "id"].ShouldBe("emotesv2_9e8516dacaa44f6181451f6e99666a9e");
        gifResult.Metadata["emote", "price"].ShouldBe("$4.99");
        gifResult.Metadata["emote", "set_id"].ShouldBe("311162030");
        gifResult.Metadata["emote", "tier"].ShouldBe("1000");
        gifResult.Metadata["emote", "token"].ShouldBe("utonyaUtopad");
        gifResult.Metadata["emote", "asset_type"].ShouldBe("ANIMATED");
        gifResult.Metadata["file", "extension"].ShouldBe("gif");

        var pngResult = results.Single(r => r.ItemId == "emotesv2_710f769a1dfd41b7ba0358a1f02037eb");
        pngResult.Uri.AbsoluteUri.ShouldBe("https://static-cdn.jtvnw.net/emoticons/v2/emotesv2_710f769a1dfd41b7ba0358a1f02037eb/default/dark/1.0");
        pngResult.Metadata!["emote", "id"].ShouldBe("emotesv2_710f769a1dfd41b7ba0358a1f02037eb");
        pngResult.Metadata["emote", "price"].ShouldBe("$4.99");
        pngResult.Metadata["emote", "set_id"].ShouldBe("496027315");
        pngResult.Metadata["emote", "tier"].ShouldBe("1000");
        pngResult.Metadata["emote", "token"].ShouldBe("utonyaHeart");
        pngResult.Metadata["emote", "asset_type"].ShouldBe("STATIC");
        pngResult.Metadata["file", "extension"].ShouldBe("png");
    }

    private static object GetGraphQlResponse(JsonElement element)
    {
        var request = (Dictionary<string, object>)JsonUtils.GetJsonElementValue(element, out _)!;
        var operation = request["operationName"].ToString();
        var variables = (Dictionary<string, object>)request["variables"];

        if (operation == "FilterableVideoTower_Videos")
        {
            var channelName = variables["channelOwnerLogin"].ToString()!;

            if (variables.TryGetValue("cursor", out var cursor))
            {
                var cursorHash = MD5.HashData(Encoding.UTF8.GetBytes(cursor.ToString()!)).ToHexString();
                return GetMockedGraphQlData(operation, channelName, cursorHash);
            }
            else
            {
                return GetMockedGraphQlData(operation, channelName);
            }
        }
        else if (operation == "ChannelVideoCore")
        {
            var videoId = variables["videoID"].ToString()!;
            return GetMockedGraphQlData(operation, videoId);
        }
        else if (operation == "VideoMetadata")
        {
            var channelName = variables["channelLogin"].ToString()!;
            var videoId = variables["videoID"].ToString()!;
            return GetMockedGraphQlData(operation, channelName, videoId);
        }
        else if (operation == "ClipsCards__User")
        {
            var channelName = variables["login"].ToString()!;

            if (variables.TryGetValue("cursor", out var cursor))
            {
                return GetMockedGraphQlData(operation, channelName, cursor.ToString()!);
            }
            else
            {
                return GetMockedGraphQlData(operation, channelName);
            }
        }
        else if (operation == "VideoAccessToken_Clip")
        {
            var slug = variables["slug"].ToString()!;
            return GetMockedGraphQlData(operation, slug);
        }
        else if (operation == "ClipsSocialShare")
        {
            var slug = variables["slug"].ToString()!;
            return GetMockedGraphQlData(operation, slug);
        }
        else if (operation == "ComscoreStreamingQuery")
        {
            var slug = variables["clipSlug"].ToString()!;
            return GetMockedGraphQlData(operation, slug);
        }
        else if (operation == "ClipsBroadcasterInfo")
        {
            var slug = variables["slug"].ToString()!;
            return GetMockedGraphQlData(operation, slug);
        }
        else if (operation == "ClipsViewCount")
        {
            var slug = variables["slug"].ToString()!;
            return GetMockedGraphQlData(operation, slug);
        }
        else if (operation == "ClipsCurator")
        {
            var slug = variables["slug"].ToString()!;
            return GetMockedGraphQlData(operation, slug);
        }
        else if (operation == "ChannelShell")
        {
            var channelName = variables["login"].ToString()!;
            return GetMockedGraphQlData(operation, channelName);
        }
        else if (operation == "EmotePicker_EmotePicker_UserSubscriptionProducts")
        {
            var channelId = variables["channelOwnerID"].ToString()!;
            return GetMockedGraphQlData(operation, channelId);
        }
        else
        {
            throw new InvalidOperationException();
        }
    }

    private static object GetMockedGraphQlData(params string[] parts)
    {
        return GetObjectFromManifestResource($"{MockedDataResourcePrefix}.{string.Join('.', parts)}.json");
    }

    private static object GetObjectFromManifestResource(string name)
    {
        var stream = Assembly.GetCallingAssembly().GetManifestResourceStream(name);
        if (stream == null)
        {
            throw new ArgumentException($"Assembly manifest resouce was not found for {name}", nameof(name));
        }

        var json = JsonSerializer.Deserialize<JsonElement>(stream);
        return json.EnumerateArray().First();
    }
}
