using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Web;
using Wilgysef.Stalk.Core.MetadataObjects;
using Wilgysef.Stalk.Extractors.TestBase;

namespace Wilgysef.Stalk.Extractors.Twitter.Tests;

public class TwitterExtractorTest : BaseTest
{
    private const string MockedDataResourcePrefix = "Wilgysef.Stalk.Extractors.Twitter.Tests.MockedData";

    private static readonly Regex UserByScreenNameRegex = new(@"^https://twitter\.com/i/api/graphql/[A-Za-z0-9_]+/UserByScreenName", RegexOptions.Compiled);
    private static readonly Regex UserTweetsRegex = new(@"^https://twitter\.com/i/api/graphql/[A-Za-z0-9_]+/UserTweets", RegexOptions.Compiled);
    private static readonly Regex TweetDetailRegex = new(@"^https://twitter\.com/i/api/graphql/[A-Za-z0-9_]+/TweetDetail", RegexOptions.Compiled);

    private readonly TwitterExtractor _twitterExtractor;

    public TwitterExtractorTest()
    {
        var messageHandler = new MockHttpMessageHandler();
        messageHandler
            .AddEndpoint(UserByScreenNameRegex, (request, cancellationToken) =>
            {
                var json = GetUriQueryVariables(request.RequestUri!);
                var userScreenName = json["screen_name"];
                return GetResponseMessageFromManifestResource($"{MockedDataResourcePrefix}.UserByScreenName.{userScreenName}.json");
            })
            .AddEndpoint(UserTweetsRegex, (request, cancellationToken) =>
            {
                var json = GetUriQueryVariables(request.RequestUri!);
                var userId = json["userId"];
                var cursor = json["cursor"];

                if (cursor == null)
                {
                    return GetResponseMessageFromManifestResource($"{MockedDataResourcePrefix}.UserTweets.{userId}.json");
                }
                return GetResponseMessageFromManifestResource($"{MockedDataResourcePrefix}.UserTweets.{userId}.{cursor}.json");
            })
            .AddEndpoint(TweetDetailRegex, (request, cancellationToken) =>
            {
                var json = GetUriQueryVariables(request.RequestUri!);
                var tweetId = json["focalTweetId"];
                return GetResponseMessageFromManifestResource($"{MockedDataResourcePrefix}.TweetDetail.{tweetId}.json");
            });

        _twitterExtractor = new TwitterExtractor(new HttpClient(messageHandler));
    }

    [Fact]
    public async Task Get_User_Tweets()
    {
        var results = await _twitterExtractor.ExtractAsync(
            new Uri("https://twitter.com/amatsukauto"),
            null!,
            new MetadataObject('.')).ToListAsync();
    }

    [Fact]
    public async Task Get_Tweet_Image()
    {
        await foreach (var result in _twitterExtractor.ExtractAsync(
            new Uri("https://twitter.com/amatsukauto/status/1560187874460733440"),
            null!,
            new MetadataObject('.')))
        {

        }
    }

    [Fact]
    public async Task Get_Tweet_Text_Url()
    {
        await foreach (var result in _twitterExtractor.ExtractAsync(
            new Uri("https://twitter.com/amatsukauto/status/1554680837861683200"),
            null!,
            new MetadataObject('.')))
        {

        }
    }

    [Fact]
    public async Task Get_Tweet_Text_Video()
    {
        await foreach (var result in _twitterExtractor.ExtractAsync(
            new Uri("https://twitter.com/amatsukauto/status/1523276529123397632"),
            null!,
            new MetadataObject('.')))
        {

        }
    }

    private static Task<HttpResponseMessage> GetResponseMessageFromManifestResource(string name, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
        if (stream == null)
        {
            throw new ArgumentException($"Assembly manifest resouce was not found for {name}", nameof(name));
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(stream),
        });
    }

    private static JsonNode GetUriQueryVariables(Uri uri)
    {
        var query = HttpUtility.ParseQueryString(uri.Query);
        return JsonSerializer.Deserialize<JsonNode>(query["variables"]!)!;
    }
}
