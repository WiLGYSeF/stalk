using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Wilgysef.Stalk.TwitterExtractors.Tests;

internal class TwitterHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (new Regex(@"https://twitter\.com/i/api/graphql/[A-Za-z0-9_]+/UserByScreenName", RegexOptions.Compiled).IsMatch(request.RequestUri.AbsoluteUri))
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Wilgysef.Stalk.TwitterExtractors.Tests.MockedData.UserByScreenName.json");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(stream),
            });
        }
        if (new Regex(@"https://twitter\.com/i/api/graphql/[A-Za-z0-9_]+/TweetDetail", RegexOptions.Compiled).IsMatch(request.RequestUri.AbsoluteUri))
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Wilgysef.Stalk.TwitterExtractors.Tests.MockedData.TweetDetail.Image.json");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(stream),
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
