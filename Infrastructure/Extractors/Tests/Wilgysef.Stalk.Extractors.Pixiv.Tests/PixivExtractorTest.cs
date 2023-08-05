using Shouldly;
using System.Net;
using System.Text.RegularExpressions;
using Wilgysef.HttpClientInterception;
using Wilgysef.Stalk.Core.MetadataObjects;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Extractors.TestBase;
using Wilgysef.Stalk.TestBase.Shared;

namespace Wilgysef.Stalk.Extractors.Pixiv.Tests;

public class PixivExtractorTest : BaseTest
{
    private static readonly string MockedDataResourcePrefix = $"{typeof(PixivExtractorTest).Namespace}.MockedData";

    private static readonly Regex ArtworkRegex = new(@"^(?:https?://)?(?:www\.)?pixiv\.net/(?<language>[A-Za-z]+)/artworks/(?<id>[0-9]+)(?:/|$)", RegexOptions.Compiled);
    private static readonly Regex IllustRegex = new(@"^https://www\.pixiv\.net/ajax/illust/(?<artworkId>[0-9]+)/pages", RegexOptions.Compiled);
    private static readonly Regex ProfileRegex = new(@"^https://www\.pixiv\.net/ajax/user/(?<userId>[0-9]+)/profile/all", RegexOptions.Compiled);

    private readonly PixivExtractor _pixivExtractor;

    private readonly HttpClientInterceptor _httpInterceptor;
    
    public PixivExtractorTest()
    {
        _httpInterceptor = HttpClientInterceptor.Create();
        _httpInterceptor
            .AddForAny(_ => new HttpResponseMessage(HttpStatusCode.NotFound))
            .AddUri(ArtworkRegex, request =>
            {
                PixivUri.TryGetUri(request.RequestUri!, out var uri);
                return HttpUtilities.GetResponseMessageFromManifestResource($"{MockedDataResourcePrefix}.Artwork.{uri.ArtworkId}.html");
            })
            .AddUri(IllustRegex, request =>
            {
                var match = IllustRegex.Match(request.RequestUri!.AbsoluteUri);
                return HttpUtilities.GetResponseMessageFromManifestResource($"{MockedDataResourcePrefix}.Artwork.{match.Groups["artworkId"].Value}.json");
            })
            .AddUri(ProfileRegex, request =>
            {
                var match = ProfileRegex.Match(request.RequestUri!.AbsoluteUri);
                return HttpUtilities.GetResponseMessageFromManifestResource($"{MockedDataResourcePrefix}.Profile.{match.Groups["userId"].Value}.json");
            });

        _pixivExtractor = new(new HttpClient(_httpInterceptor));
    }

    [Theory]
    [InlineData("https://www.pixiv.net/en/artworks/86466485", true)]
    [InlineData("https://www.pixiv.net/en/users/46503769/artworks", true)]
    public void Can_Extract(string uri, bool expected)
    {
        _pixivExtractor.CanExtract(new Uri(uri)).ShouldBe(expected);
    }

    [Theory]
    [InlineData("https://www.pixiv.net/en/artworks/86466485", "artwork#86466485")]
    [InlineData("https://www.pixiv.net/en/users/46503769", null)]
    public void GetItemIds(string uri, string expected)
    {
        _pixivExtractor.GetItemId(new Uri(uri)).ShouldBe(expected);
    }

    [Fact]
    public async Task Get_Artwork_Single()
    {
        var results = await _pixivExtractor.ExtractAsync(
            new Uri("https://www.pixiv.net/en/artworks/86466485"),
            null,
            new MetadataObject()).ToListAsync();

        results.Count.ShouldBe(1);
        var result = results[0];
        result.ItemId.ShouldBe("artwork#86466485");
        result.Uri.ToString().ShouldBe("https://i.pximg.net/img-original/img/2020/12/23/01/21/07/86466485_p0.png");
        result.Type.ShouldBe(JobTaskType.Download);
        result.DownloadRequestData!.Headers!.Count.ShouldBe(1);
        result.DownloadRequestData!.Headers!.Single(p => p.Key == "Referer").Value.ShouldBe("https://www.pixiv.net");
        result.Metadata!["artwork", "id"].ShouldBe("86466485");
        result.Metadata!["artwork", "title"].ShouldBe("天使うと");
        result.Metadata!["artwork", "description"].ShouldBe("遅くなったけど天使うと（&lt;br /&gt;@amatsukauto&lt;br /&gt;）デビュー＆10万人おめでとうございます！！");
        result.Metadata!["artwork", "illustType"].ShouldBe(0);
        result.Metadata!["artwork", "createDate"].ShouldBe("2020-12-22 11:21:00");
        result.Metadata!["artwork", "uploadDate"].ShouldBe("2020-12-22 11:21:00");
        result.Metadata!["artwork", "restrict"].ShouldBe(0);
        result.Metadata!["artwork", "xRestrict"].ShouldBe(0);

        var tags = (List<Dictionary<string, object?>>)result.Metadata!["artwork", "tags"]!;
        tags.Count.ShouldBe(8);
        tags[0]["tag"].ShouldBe("オリジナル");
        tags[0]["romaji"].ShouldBe("orijinaru");
        ((Dictionary<string, string>)tags[0]["translation"]!)["en"].ShouldBe("original");

        result.Metadata!["artwork", "user", "id"].ShouldBe("28145748");
        result.Metadata!["artwork", "user", "name"].ShouldBe("Nabi");
        result.Metadata!["artwork", "user", "account"].ShouldBe("uz02");
        result.Metadata!["artwork", "bookmarkCount"].ShouldBe(23774);
        result.Metadata!["artwork", "likeCount"].ShouldBe(15661);
        result.Metadata!["artwork", "commentCount"].ShouldBe(73);
        result.Metadata!["artwork", "responseCount"].ShouldBe(0);
        result.Metadata!["artwork", "viewCount"].ShouldBe(140632);
        result.Metadata!["artwork", "isHowto"].ShouldBe(false);
        result.Metadata!["artwork", "isOriginal"].ShouldBe(true);
        result.Metadata!["artwork", "isUnlisted"].ShouldBe(false);
        result.Metadata!["artwork", "aiType"].ShouldBe(0);
        result.Metadata!["artwork", "width"].ShouldBe(900);
        result.Metadata!["artwork", "height"].ShouldBe(1367);
        result.Metadata!["file", "extension"].ShouldBe("png");
    }

    [Fact]
    public async Task Get_Artwork_Multiple()
    {
        var results = await _pixivExtractor.ExtractAsync(
            new Uri("https://www.pixiv.net/en/artworks/88581689"),
            null,
            new MetadataObject()).ToListAsync();

        results.Count.ShouldBe(2);
        var first = results[0];
        first.ItemId.ShouldBe("artwork#88581689#1");
        first.Uri.ToString().ShouldBe("https://i.pximg.net/img-original/img/2021/03/20/20/00/53/88581689_p0.jpg");
        first.Type.ShouldBe(JobTaskType.Download);
        first.DownloadRequestData!.Headers!.Count.ShouldBe(1);
        first.DownloadRequestData!.Headers!.Single(p => p.Key == "Referer").Value.ShouldBe("https://www.pixiv.net");
        first.Metadata!["artwork", "id"].ShouldBe("88581689");
        first.Metadata!["artwork", "title"].ShouldBe("可愛い天使");
        first.Metadata!["artwork", "description"].ShouldBe("&lt;a href=\"/jump.php?https%3A%2F%2Ftwitter.com%2Fxdeyuix%2Fstatus%2F1373227824098635779\" target=\"_blank\"&gt;https://twitter.com/xdeyuix/status/1373227824098635779&lt;/a&gt;&lt;br /&gt;&lt;a href=\"/jump.php?https%3A%2F%2Fwww.instagram.com%2Fp%2FCMo1n4Bhfso%2F\" target=\"_blank\"&gt;https://www.instagram.com/p/CMo1n4Bhfso/&lt;/a&gt;");
        first.Metadata!["artwork", "illustType"].ShouldBe(0);
        first.Metadata!["artwork", "createDate"].ShouldBe("2021-03-20 07:00:00");
        first.Metadata!["artwork", "uploadDate"].ShouldBe("2021-03-20 07:00:00");
        first.Metadata!["artwork", "restrict"].ShouldBe(0);
        first.Metadata!["artwork", "xRestrict"].ShouldBe(0);

        var tags = (List<Dictionary<string, object?>>)first.Metadata!["artwork", "tags"]!;
        tags.Count.ShouldBe(9);
        tags[0]["tag"].ShouldBe("utoart");
        tags[1]["tag"].ShouldBe("天使うと");
        tags[1]["romaji"].ShouldBe("amatsukauto");
        ((Dictionary<string, string>)tags[1]["translation"]!)["en"].ShouldBe("Amatsuka Uto");

        first.Metadata!["artwork", "user", "id"].ShouldBe("14881241");
        first.Metadata!["artwork", "user", "name"].ShouldBe("Deyui | デユイ");
        first.Metadata!["artwork", "user", "account"].ShouldBe("dewoo");
        first.Metadata!["artwork", "bookmarkCount"].ShouldBe(3758);
        first.Metadata!["artwork", "likeCount"].ShouldBe(2362);
        first.Metadata!["artwork", "commentCount"].ShouldBe(15);
        first.Metadata!["artwork", "responseCount"].ShouldBe(0);
        first.Metadata!["artwork", "viewCount"].ShouldBe(15762);
        first.Metadata!["artwork", "isHowto"].ShouldBe(false);
        first.Metadata!["artwork", "isOriginal"].ShouldBe(false);
        first.Metadata!["artwork", "isUnlisted"].ShouldBe(false);
        first.Metadata!["artwork", "aiType"].ShouldBe(0);
        first.Metadata!["artwork", "width"].ShouldBe(1920);
        first.Metadata!["artwork", "height"].ShouldBe(1257);
        first.Metadata!["file", "extension"].ShouldBe("jpg");

        var second = results[1];
        second.ItemId.ShouldBe("artwork#88581689#2");
        second.Uri.ToString().ShouldBe("https://i.pximg.net/img-original/img/2021/03/20/20/00/53/88581689_p1.jpg");
        second.Type.ShouldBe(JobTaskType.Download);
        second.DownloadRequestData!.Headers!.Count.ShouldBe(1);
        second.DownloadRequestData!.Headers!.Single(p => p.Key == "Referer").Value.ShouldBe("https://www.pixiv.net");
        second.Metadata!["artwork", "id"].ShouldBe("88581689");
        second.Metadata!["artwork", "title"].ShouldBe("可愛い天使");
        second.Metadata!["artwork", "description"].ShouldBe("&lt;a href=\"/jump.php?https%3A%2F%2Ftwitter.com%2Fxdeyuix%2Fstatus%2F1373227824098635779\" target=\"_blank\"&gt;https://twitter.com/xdeyuix/status/1373227824098635779&lt;/a&gt;&lt;br /&gt;&lt;a href=\"/jump.php?https%3A%2F%2Fwww.instagram.com%2Fp%2FCMo1n4Bhfso%2F\" target=\"_blank\"&gt;https://www.instagram.com/p/CMo1n4Bhfso/&lt;/a&gt;");
        second.Metadata!["artwork", "illustType"].ShouldBe(0);
        second.Metadata!["artwork", "createDate"].ShouldBe("2021-03-20 07:00:00");
        second.Metadata!["artwork", "uploadDate"].ShouldBe("2021-03-20 07:00:00");
        second.Metadata!["artwork", "restrict"].ShouldBe(0);
        second.Metadata!["artwork", "xRestrict"].ShouldBe(0);

        tags = (List<Dictionary<string, object?>>)second.Metadata!["artwork", "tags"]!;
        tags.Count.ShouldBe(9);
        tags[0]["tag"].ShouldBe("utoart");
        tags[1]["tag"].ShouldBe("天使うと");
        tags[1]["romaji"].ShouldBe("amatsukauto");
        ((Dictionary<string, string>)tags[1]["translation"]!)["en"].ShouldBe("Amatsuka Uto");

        second.Metadata!["artwork", "user", "id"].ShouldBe("14881241");
        second.Metadata!["artwork", "user", "name"].ShouldBe("Deyui | デユイ");
        second.Metadata!["artwork", "user", "account"].ShouldBe("dewoo");
        second.Metadata!["artwork", "bookmarkCount"].ShouldBe(3758);
        second.Metadata!["artwork", "likeCount"].ShouldBe(2362);
        second.Metadata!["artwork", "commentCount"].ShouldBe(15);
        second.Metadata!["artwork", "responseCount"].ShouldBe(0);
        second.Metadata!["artwork", "viewCount"].ShouldBe(15762);
        second.Metadata!["artwork", "isHowto"].ShouldBe(false);
        second.Metadata!["artwork", "isOriginal"].ShouldBe(false);
        second.Metadata!["artwork", "isUnlisted"].ShouldBe(false);
        second.Metadata!["artwork", "aiType"].ShouldBe(0);
        second.Metadata!["artwork", "width"].ShouldBe(1920);
        second.Metadata!["artwork", "height"].ShouldBe(1257);
        second.Metadata!["file", "extension"].ShouldBe("jpg");
    }

    [Fact]
    public async Task Get_UserProfile_Artworks()
    {
        var results = await _pixivExtractor.ExtractAsync(
            new Uri("https://www.pixiv.net/en/users/46503769/artworks"),
            null,
            new MetadataObject()).ToListAsync();

        results.Count.ShouldBe(71);
        results[0].ItemId.ShouldBe("109607213");
        results[0].Uri.ToString().ShouldBe("https://www.pixiv.net/en/artworks/109607213");
        results[0].Type.ShouldBe(JobTaskType.Extract);
    }

    [Fact]
    public async Task Get_Artwork_Cookie()
    {
        _pixivExtractor.Config[PixivExtractorConfig.CookiesKey] = "testcookie";

        HttpRequestMessage request = null!;
        _httpInterceptor.RequestProcessed += RequestProcessedEventHandler;

        var results = await _pixivExtractor.ExtractAsync(
            new Uri("https://www.pixiv.net/en/artworks/86466485"),
            null,
            new MetadataObject()).ToListAsync();

        request.Headers.GetValues("Cookie").Single().ShouldBe("testcookie");

        void RequestProcessedEventHandler(object? sender, HttpRequestMessage e)
        {
            request = e;
        }
    }
}
