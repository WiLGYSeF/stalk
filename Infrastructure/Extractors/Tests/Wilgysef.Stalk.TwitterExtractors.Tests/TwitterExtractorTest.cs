using Wilgysef.Stalk.Core.MetadataObjects;

namespace Wilgysef.Stalk.TwitterExtractors.Tests;

public class TwitterExtractorTest
{
    private readonly TwitterExtractor _twitterExtractor;

    public TwitterExtractorTest()
    {
        _twitterExtractor = new TwitterExtractor(new HttpClient(new TwitterHttpMessageHandler()));
    }

    [Fact]
    public async Task Test1()
    {
        await foreach (var result in _twitterExtractor.ExtractAsync(
            new Uri("https://twitter.com/amatsukauto/status/1560187874460733440"),
            null,
            new MetadataObject('.')))
        {

        }
    }
}
