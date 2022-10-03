using Shouldly;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Wilgysef.HttpClientInterception;
using Wilgysef.Stalk.Core.MetadataObjects;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Extractors.TestBase;
using Wilgysef.Stalk.TestBase.Shared;

namespace Wilgysef.Stalk.Extractors.Twitter.Tests;

public class TwitterExtractorTest : BaseTest
{
    private static readonly string MockedDataResourcePrefix = $"{typeof(TwitterExtractorTest).Namespace}.MockedData";

    private static readonly Regex UserByScreenNameRegex = new(@"^https://twitter\.com/i/api/graphql/[A-Za-z0-9_]+/UserByScreenName", RegexOptions.Compiled);
    private static readonly Regex UserTweetsRegex = new(@"^https://twitter\.com/i/api/graphql/[A-Za-z0-9_]+/UserTweets", RegexOptions.Compiled);
    private static readonly Regex TweetDetailRegex = new(@"^https://twitter\.com/i/api/graphql/[A-Za-z0-9_]+/TweetDetail", RegexOptions.Compiled);

    private const string GuestTokenEndpoint = "https://api.twitter.com/1.1/guest/activate.json";

    private readonly TwitterExtractor _twitterExtractor;

    public TwitterExtractorTest()
    {
        var interceptor = HttpClientInterceptor.Create();
        interceptor
            .AddUri(UserByScreenNameRegex, request =>
            {
                var json = GetUriQueryVariables(request.RequestUri!);
                var userScreenName = json["screen_name"];
                return HttpUtilities.GetResponseMessageFromManifestResource($"{MockedDataResourcePrefix}.UserByScreenName.{userScreenName}.json");
            })
            .AddUri(UserTweetsRegex, request =>
            {
                var json = GetUriQueryVariables(request.RequestUri!);
                var userId = json["userId"];
                var cursor = json["cursor"];

                return cursor == null
                    ? HttpUtilities.GetResponseMessageFromManifestResource($"{MockedDataResourcePrefix}.UserTweets.{userId}.json")
                    : HttpUtilities.GetResponseMessageFromManifestResource($"{MockedDataResourcePrefix}.UserTweets.{userId}.{cursor}.json");
            })
            .AddUri(TweetDetailRegex, request =>
            {
                var json = GetUriQueryVariables(request.RequestUri!);
                var tweetId = json["focalTweetId"];
                return HttpUtilities.GetResponseMessageFromManifestResource($"{MockedDataResourcePrefix}.TweetDetail.{tweetId}.json");
            })
            .AddUri(GuestTokenEndpoint, request =>
            {
                return HttpUtilities.GetResponseMessageFromManifestResource($"{MockedDataResourcePrefix}.Activate.json");
            });

        _twitterExtractor = new TwitterExtractor(new HttpClient(interceptor));
    }

    [Theory]
    [InlineData("https://twitter.com/amatsukauto", true)]
    [InlineData("https://twitter.com/amatsukauto/", true)]
    [InlineData("https://twitter.com/amatsukauto/status/1560187874460733440", true)]
    [InlineData("https://mobile.twitter.com/amatsukauto/status/1560187874460733440", true)]
    public void Can_Extract(string uri, bool expected)
    {
        _twitterExtractor.CanExtract(new Uri(uri)).ShouldBe(expected);
    }

    [Fact]
    public async Task Get_User_Tweets()
    {
        var results = await _twitterExtractor.ExtractAsync(
            new Uri("https://twitter.com/amatsukauto"),
            null,
            new MetadataObject('.')).ToListAsync();

        results.Count.ShouldBe(99);
        results.Select(r => r.Uri.AbsoluteUri).ToHashSet().Count.ShouldBe(results.Count);
    }

    [Fact]
    public async Task Get_Tweet_Image()
    {
        var results = await _twitterExtractor.ExtractAsync(
            new Uri("https://twitter.com/amatsukauto/status/1560187874460733440"),
            null,
            new MetadataObject('.')).ToListAsync();

        results.Count.ShouldBe(1);
        var result = results.Single();
        result.ItemId.ShouldBe("1308334634745249793#1560187874460733440#1560187870648082433");
        result.Uri.AbsoluteUri.ShouldBe("https://pbs.twimg.com/media/FabmmiTaIAEO4zM?format=jpg&name=large");
        result.Metadata!["created_at"].ShouldBe(new DateTime(2022, 8, 18, 8, 52, 30));
        result.Metadata["favorite_count"].ShouldBe(17340);
        result.Metadata["file.extension"].ShouldBe("jpg");
        result.Metadata["is_quote_status"].ShouldBe(false);
        result.Metadata["lang"].ShouldBe("zxx");
        result.Metadata["media_id"].ShouldBe("1560187870648082433");
        result.Metadata["possibly_sensitive"].ShouldBe(false);
        result.Metadata["quote_count"].ShouldBe(13);
        result.Metadata["reply_count"].ShouldBe(103);
        result.Metadata["retweet_count"].ShouldBe(1114);
        result.Metadata["tweet_id"].ShouldBe("1560187874460733440");
        result.Metadata["user.id"].ShouldBe("1308334634745249793");
        result.Metadata["user.screen_name"].ShouldBe("amatsukauto");
        result.Type.ShouldBe(JobTaskType.Download);
    }

    [Fact]
    public async Task Get_Tweet_Text_Url()
    {
        var results = await _twitterExtractor.ExtractAsync(
            new Uri("https://twitter.com/amatsukauto/status/1554680837861683200"),
            null,
            new MetadataObject('.')).ToListAsync();

        results.Count.ShouldBe(1);
        var result = results.Single();
        result.ItemId.ShouldBe("1308334634745249793#1554680837861683200");
        result.Uri.AbsoluteUri.ShouldBe("data:;base64,U3BsYXRvb24yIOOBj+OCszrlvaEgaHR0cHM6Ly90LmNvL0VXYkJQVG1FNEwKaHR0cHM6Ly93d3cudHdpdGNoLnR2L3V0b255YW4=");
        result.Metadata!["created_at"].ShouldBe(new DateTime(2022, 8, 3, 4, 9, 30));
        result.Metadata["favorite_count"].ShouldBe(2022);
        result.Metadata["file.extension"].ShouldBe("txt");
        result.Metadata["is_quote_status"].ShouldBe(false);
        result.Metadata["lang"].ShouldBe("ja");
        result.Metadata["origin.uri"].ShouldBe("https://twitter.com/amatsukauto/status/1554680837861683200");
        result.Metadata["possibly_sensitive"].ShouldBe(false);
        result.Metadata["quote_count"].ShouldBe(0);
        result.Metadata["reply_count"].ShouldBe(17);
        result.Metadata["retweet_count"].ShouldBe(129);
        result.Metadata["tweet_id"].ShouldBe("1554680837861683200");
        result.Metadata["user.id"].ShouldBe("1308334634745249793");
        result.Metadata["user.screen_name"].ShouldBe("amatsukauto");
        result.Type.ShouldBe(JobTaskType.Download);
    }

    [Fact]
    public async Task Get_Tweet_Text_Video()
    {
        var results = await _twitterExtractor.ExtractAsync(
            new Uri("https://twitter.com/amatsukauto/status/1523276529123397632"),
            null,
            new MetadataObject('.')).ToListAsync();

        results.Count.ShouldBe(3);
        var textResult = results.Single(r => r.Uri.AbsoluteUri.StartsWith("data:"));
        textResult.ItemId.ShouldBe("1308334634745249793#1523276529123397632");
        textResult.Uri.AbsoluteUri.ShouldBe("data:;base64,5paw44GX44GE44Kr44OQ44O844KS5oqV56i/44GX44G+44GX44Gf4p2VCuOBi+OBo+OBk+OBhOOBhOOBruOBp+OBn+OBj+OBleOCk+iBnuOBhOOBpuOBj+OBoOOBleOBhOODvO+8geKZoQpmdWxs77yaaHR0cHM6Ly90LmNvL25sTE9ZbENtbE0KClZvY2Fs77ya44OK44OK44Kr44Kw44OpLCDlpKnkvb/jgYbjgagKSWxsdXN077yaSmFueWhlcm8gICDmp5gKTWl477yaUGQuNDbjgIDmp5gKTW92aWXvvJpSaWVzeiAg5qeYCmh0dHBzOi8veW91dHUuYmUvSEJrdExUeUxMOVU=");
        textResult.Metadata!["created_at"].ShouldBe(new DateTime(2022, 5, 8, 12, 20, 0));
        textResult.Metadata["favorite_count"].ShouldBe(5823);
        textResult.Metadata["file.extension"].ShouldBe("txt");
        textResult.Metadata["is_quote_status"].ShouldBe(false);
        textResult.Metadata["lang"].ShouldBe("ja");
        textResult.Metadata["possibly_sensitive"].ShouldBe(false);
        textResult.Metadata["quote_count"].ShouldBe(33);
        textResult.Metadata["reply_count"].ShouldBe(116);
        textResult.Metadata["retweet_count"].ShouldBe(912);
        textResult.Metadata["tweet_id"].ShouldBe("1523276529123397632");
        textResult.Metadata["user.id"].ShouldBe("1308334634745249793");
        textResult.Metadata["user.screen_name"].ShouldBe("amatsukauto");
        textResult.Type.ShouldBe(JobTaskType.Download);

        var thumbnailResult = results.Single(r => r.Uri.AbsoluteUri.StartsWith("https://pbs.twimg.com/ext_tw_video_thumb"));
        thumbnailResult.ItemId.ShouldBe("1308334634745249793#1523276529123397632#1523196911448035328");
        thumbnailResult.Uri.AbsoluteUri.ShouldBe("https://pbs.twimg.com/ext_tw_video_thumb/1523196911448035328/pu/img/IjK77EYau0_-qDCu.jpg");
        thumbnailResult.Metadata!["created_at"].ShouldBe(new DateTime(2022, 5, 8, 12, 20, 0));
        thumbnailResult.Metadata["favorite_count"].ShouldBe(5823);
        thumbnailResult.Metadata["file.extension"].ShouldBe("jpg");
        thumbnailResult.Metadata["is_quote_status"].ShouldBe(false);
        thumbnailResult.Metadata["lang"].ShouldBe("ja");
        thumbnailResult.Metadata["media_id"].ShouldBe("1523196911448035328");
        thumbnailResult.Metadata["possibly_sensitive"].ShouldBe(false);
        thumbnailResult.Metadata["quote_count"].ShouldBe(33);
        thumbnailResult.Metadata["reply_count"].ShouldBe(116);
        thumbnailResult.Metadata["retweet_count"].ShouldBe(912);
        thumbnailResult.Metadata["tweet_id"].ShouldBe("1523276529123397632");
        thumbnailResult.Metadata["user.id"].ShouldBe("1308334634745249793");
        thumbnailResult.Metadata["user.screen_name"].ShouldBe("amatsukauto");
        thumbnailResult.Type.ShouldBe(JobTaskType.Download);

        var videoResult = results.Single(r => r.Uri.AbsoluteUri.StartsWith("https://video.twimg.com/ext_tw_video/"));
        videoResult.ItemId.ShouldBe("1308334634745249793#1523276529123397632#1523196911448035328");
        videoResult.Uri.AbsoluteUri.ShouldBe("https://video.twimg.com/ext_tw_video/1523196911448035328/pu/vid/1280x720/r-Ybk23JsBkJIy9b.mp4?tag=12");
        videoResult.Metadata!["created_at"].ShouldBe(new DateTime(2022, 5, 8, 12, 20, 0));
        videoResult.Metadata["favorite_count"].ShouldBe(5823);
        videoResult.Metadata["file.extension"].ShouldBe("mp4");
        videoResult.Metadata["is_quote_status"].ShouldBe(false);
        videoResult.Metadata["lang"].ShouldBe("ja");
        videoResult.Metadata["media_id"].ShouldBe("1523196911448035328");
        videoResult.Metadata["possibly_sensitive"].ShouldBe(false);
        videoResult.Metadata["quote_count"].ShouldBe(33);
        videoResult.Metadata["reply_count"].ShouldBe(116);
        videoResult.Metadata["retweet_count"].ShouldBe(912);
        videoResult.Metadata["tweet_id"].ShouldBe("1523276529123397632");
        videoResult.Metadata["user.id"].ShouldBe("1308334634745249793");
        videoResult.Metadata["user.screen_name"].ShouldBe("amatsukauto");
        videoResult.Metadata["video.bitrate"].ShouldBe(2176000);
        videoResult.Metadata["video.duration_millis"].ShouldBe(26541);
        videoResult.Metadata["video.view_count"].ShouldBe(76821);
        videoResult.Type.ShouldBe(JobTaskType.Download);
    }

    [Fact]
    public async Task Get_Tweet_Retweet()
    {
        var results = await _twitterExtractor.ExtractAsync(
            new Uri("https://twitter.com/amatsukauto/status/1567680068113285121"),
            null,
            new MetadataObject('.')).ToListAsync();

        results.Count.ShouldBe(2);
        var result = results.Single(r => r.Uri.AbsoluteUri.StartsWith("data:"));
        result.ItemId.ShouldBe("1308334634745249793#1567680068113285121");
        result.Uri.AbsoluteUri.ShouldBe("data:;base64,UlQgQEFzdGVyQXJjYWRpYTogRElWSU5FIERVTyBUQUtJTkcgT1ZFUiBBUEVYIExFR0VORFMhCkdhbWluZyB3LyDimIHvuI9AYW1hdHN1a2F1dG8gCgrwn5KraHR0cHM6Ly90LmNvL0NXZnhtMFlVTXggaHR0cHM6Ly90LmNvL0tYTnB5THhQeQpodHRwOi8veW91dHUuYmUvM0Vobm1rWjhPQlE=");
        result.Metadata!["created_at"].ShouldBe(new DateTime(2022, 9, 8, 1, 3, 48));
        result.Metadata["favorite_count"].ShouldBe(0);
        result.Metadata["file.extension"].ShouldBe("txt");
        result.Metadata["is_quote_status"].ShouldBe(false);
        result.Metadata["lang"].ShouldBe("en");
        result.Metadata["possibly_sensitive"].ShouldBe(false);
        result.Metadata["quote_count"].ShouldBe(0);
        result.Metadata["reply_count"].ShouldBe(0);
        result.Metadata["retweet_count"].ShouldBe(257);
        result.Metadata["retweet.tweet_id"].ShouldBe("1567677602781069314");
        result.Metadata["retweet.user.id"].ShouldBe("1545352592884084736");
        result.Metadata["retweet.user.screen_name"].ShouldBe("AsterArcadia");
        result.Metadata["tweet_id"].ShouldBe("1567680068113285121");
        result.Metadata["user.id"].ShouldBe("1308334634745249793");
        result.Metadata["user.screen_name"].ShouldBe("amatsukauto");
        result.Type.ShouldBe(JobTaskType.Download);
    }

    private static JsonNode GetUriQueryVariables(Uri uri)
    {
        var query = uri.GetQueryParameters();
        return JsonSerializer.Deserialize<JsonNode>(query["variables"]!)!;
    }
}
