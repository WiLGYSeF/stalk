using Shouldly;
using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Wilgysef.HttpClientInterception.Tests;

public class HttpClientInterceptorRuleBuilderTest
{
    private readonly HttpClientInterceptor _interceptor;

    public HttpClientInterceptorRuleBuilderTest()
    {
        _interceptor = HttpClientInterceptor.Create()
            .AddForAny(request => new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                RequestMessage = request
            });
    }

    [Theory]
    [InlineData("1.1", "1.1", true)]
    [InlineData("2.0", "1.1", false)]
    public async Task VersionFilter(string version, string versionFilter, bool expectedMatch)
    {
        await ShouldMatchVersionAsync(
            Version.Parse(version),
            builder => builder.ForVersion(Version.Parse(versionFilter)),
            expectedMatch);
    }

    [Theory]
    [InlineData("1.1", new[] { "1.1" }, true)]
    [InlineData("2.0", new[] { "1.1" }, false)]
    [InlineData("2.0", new[] { "1.1", "2.0" }, true)]
    public async Task VersionFilter_Params(string version, string[] versionFilter, bool expectedMatch)
    {
        var versions = new Version[versionFilter.Length];
        for (int i = 0; i < versionFilter.Length; i++)
        {
            versions[i] = Version.Parse(versionFilter[i]);
        }

        await ShouldMatchVersionAsync(
            Version.Parse(version),
            builder => builder.ForVersions(versions),
            expectedMatch);
    }

    [Theory]
    [InlineData("1.1", new[] { "1.1" }, true)]
    [InlineData("2.0", new[] { "1.1" }, false)]
    [InlineData("2.0", new[] { "1.1", "2.0" }, true)]
    public async Task VersionFilter_Enumerable(string version, string[] versionFilter, bool expectedMatch)
    {
        await ShouldMatchVersionAsync(
            Version.Parse(version),
            builder => builder.ForVersions(versionFilter.Select(v => Version.Parse(v))),
            expectedMatch);
    }

    [Theory]
    [InlineData("1.1", "1.1", "1.1", true)]
    [InlineData("1.1", "1.0", "1.2", true)]
    [InlineData("1.1", "1.0", "1.1", true)]
    [InlineData("1.1", "1.1", "1.3", true)]
    [InlineData("2.0", "1.0", "1.1", false)]
    [InlineData("2.0", "2.1", "3.0", false)]
    public async Task VersionFilter_Range(string version, string versionFilterMin, string versionFilterMax, bool expectedMatch)
    {
        await ShouldMatchVersionAsync(
            Version.Parse(version),
            builder => builder.ForVersionRange(Version.Parse(versionFilterMin), Version.Parse(versionFilterMax)),
            expectedMatch);
    }

    [Theory]
    [InlineData("GET", "GET", true)]
    [InlineData("GET", "POST", false)]
    public async Task MethodFilter(string method, string methodFilter, bool expectedMatch)
    {
        await ShouldMatchMethodAsync(
            new HttpMethod(method),
            builder => builder.ForMethod(new HttpMethod(methodFilter)),
            expectedMatch);
    }

    [Theory]
    [InlineData("GET", new[] { "GET" }, true)]
    [InlineData("GET", new[] { "POST", "GET" }, true)]
    [InlineData("GET", new[] { "POST" }, false)]
    public async Task MethodFilter_Params(string method, string[] methodFilter, bool expectedMatch)
    {
        var methods = new HttpMethod[methodFilter.Length];
        for (var i = 0; i < methodFilter.Length; i++)
        {
            methods[i] = new HttpMethod(methodFilter[i]);
        }

        await ShouldMatchMethodAsync(
            new HttpMethod(method),
            builder => builder.ForMethods(methods),
            expectedMatch);
    }

    [Theory]
    [InlineData("GET", new[] { "GET" }, true)]
    [InlineData("GET", new[] { "POST", "GET" }, true)]
    [InlineData("GET", new[] { "POST" }, false)]
    public async Task MethodFilter_Enumerable(string method, string[] methodFilter, bool expectedMatch)
    {
        await ShouldMatchMethodAsync(
            new HttpMethod(method),
            builder => builder.ForMethods(methodFilter.Select(m => new HttpMethod(m))),
            expectedMatch);
    }

    [Theory]
    [InlineData("https://example.com", "https://example.com", true)]
    [InlineData("https://example.com", "https://test.com", false)]
    public async Task UriFilter(string uri, string uriFilter, bool expectedMatch)
    {
        await ShouldMatchUriAsync(
            new Uri(uri),
            builder => builder.ForUri(new Uri(uriFilter)),
            expectedMatch);
    }

    [Theory]
    [InlineData("https://example.com", "https://example.com", true)]
    [InlineData("https://example.com", "https://example.com/", true)]
    [InlineData("https://example.com", "https://test.com", false)]
    public async Task UriFilter_String(string uri, string uriFilter, bool expectedMatch)
    {
        await ShouldMatchUriAsync(
            new Uri(uri),
            builder => builder.ForUri(uriFilter),
            expectedMatch);
    }

    [Theory]
    [InlineData("https://example.com", @"https://.*\.com", true)]
    [InlineData("https://example.com", @"https://.*\.net", false)]
    public async Task UriFilter_Regex(string uri, string uriFilter, bool expectedMatch)
    {
        await ShouldMatchUriAsync(new Uri(uri),
            builder => builder.ForUri(new Regex(uriFilter)),
            expectedMatch);
    }

    [Theory]
    [InlineData("https://example.com", "https://example.com", true)]
    [InlineData("https://example.com", "https://test.com", false)]
    public async Task UriFilter_StartsWith(string uri, string uriFilter, bool expectedMatch)
    {
        await ShouldMatchUriAsync(
            new Uri(uri),
            builder => builder.ForUriStartsWith(uriFilter),
            expectedMatch);
    }

    [Theory]
    [InlineData("https://example.com", "https", true)]
    [InlineData("https://example.com", "http", false)]
    public async Task SchemeFilter(string uri, string schemeFilter, bool expectedMatch)
    {
        await ShouldMatchUriAsync(
            new Uri(uri),
            builder => builder.ForScheme(schemeFilter),
            expectedMatch);
    }

    [Theory]
    [InlineData("https://example.com", "https?", true)]
    [InlineData("https://example.com", "file", false)]
    public async Task SchemeFilter_Regex(string uri, string schemeFilter, bool expectedMatch)
    {
        await ShouldMatchUriAsync(
            new Uri(uri),
            builder => builder.ForScheme(new Regex(schemeFilter)),
            expectedMatch);
    }

    [Theory]
    [InlineData("https://example.com", new[] { "http", "https" }, true)]
    [InlineData("https://example.com", new[] { "file" }, false)]
    public async Task SchemeFilter_Params(string uri, string[] schemeFilter, bool expectedMatch)
    {
        await ShouldMatchUriAsync(
            new Uri(uri),
            builder => builder.ForSchemes(schemeFilter),
            expectedMatch);
    }

    [Theory]
    [InlineData("https://example.com", new[] { "http", "https" }, true)]
    [InlineData("https://example.com", new[] { "file" }, false)]
    public async Task SchemeFilter_Enumerable(string uri, string[] schemeFilter, bool expectedMatch)
    {
        await ShouldMatchUriAsync(
            new Uri(uri),
            builder => builder.ForSchemes((IEnumerable<string>)schemeFilter),
            expectedMatch);
    }

    [Theory]
    [InlineData("https://example.com", "example.com", true)]
    [InlineData("https://example.com", "test.com", false)]
    [InlineData("https://www.example.com", "example.com", false)]
    [InlineData("https://www.example.com", "www.example.com", true)]
    public async Task HostFilter(string uri, string host, bool expectedMatch)
    {
        await ShouldMatchUriAsync(
            new Uri(uri),
            builder => builder.ForHost(host),
            expectedMatch);
    }

    [Theory]
    [InlineData("https://example.com", @"example\..*", true)]
    [InlineData("https://example.com", @".*\.net", false)]
    public async Task HostFilter_Regex(string uri, string host, bool expectedMatch)
    {
        await ShouldMatchUriAsync(
            new Uri(uri),
            builder => builder.ForHost(new Regex(host)),
            expectedMatch);
    }

    [Theory]
    [InlineData("https://example.com", 443, true)]
    [InlineData("https://example.com:5000", 5000, true)]
    [InlineData("https://example.com:5000", 5001, false)]
    public async Task PortFilter(string uri, int port, bool expectedMatch)
    {
        await ShouldMatchUriAsync(
            new Uri(uri),
            builder => builder.ForPort(port),
            expectedMatch);
    }

    [Theory]
    [InlineData("https://example.com", new[] { 443 }, true)]
    [InlineData("https://example.com:5000", new[] { 443, 5000 }, true)]
    [InlineData("https://example.com:5000", new[] { 5001 }, false)]
    public async Task PortFilter_Params(string uri, int[] port, bool expectedMatch)
    {
        await ShouldMatchUriAsync(
            new Uri(uri),
            builder => builder.ForPorts(port),
            expectedMatch);
    }

    [Theory]
    [InlineData("https://example.com", new[] { 443 }, true)]
    [InlineData("https://example.com:5000", new[] { 443, 5000 }, true)]
    [InlineData("https://example.com:5000", new[] { 5001 }, false)]
    public async Task PortFilter_Enumerable(string uri, int[] port, bool expectedMatch)
    {
        await ShouldMatchUriAsync(
            new Uri(uri),
            builder => builder.ForPorts((IEnumerable<int>)port),
            expectedMatch);
    }

    [Theory]
    [InlineData("https://example.com", 443, 443, true)]
    [InlineData("https://example.com", 400, 500, true)]
    [InlineData("https://example.com", 443, 500, true)]
    [InlineData("https://example.com", 400, 443, true)]
    [InlineData("https://example.com", 100, 200, false)]
    [InlineData("https://example.com", 500, 600, false)]
    public async Task PortFilter_Range(string uri, int portMin, int portMax, bool expectedMatch)
    {
        await ShouldMatchUriAsync(
            new Uri(uri),
            builder => builder.ForPortRange(portMin, portMax),
            expectedMatch);
    }

    [Theory]
    [InlineData("https://example.com", "", false)]
    [InlineData("https://example.com", "/", true)]
    [InlineData("https://example.com/test", "/test", true)]
    [InlineData("https://example.com/test", "test", false)]
    [InlineData("https://example.com/test", "asdf", false)]
    public async Task PathFilter(string uri, string path, bool expectedMatch)
    {
        await ShouldMatchUriAsync(
            new Uri(uri),
            builder => builder.ForPath(path),
            expectedMatch);
    }

    [Theory]
    [InlineData("https://example.com/test", "/t[a-e]", true)]
    [InlineData("https://example.com/test", "asdf", false)]
    public async Task PathFilter_Regex(string uri, string path, bool expectedMatch)
    {
        await ShouldMatchUriAsync(
            new Uri(uri),
            builder => builder.ForPath(new Regex(path)),
            expectedMatch);
    }

    [Theory]
    [InlineData("https://example.com", "/", true)]
    [InlineData("https://example.com/test", "/t", true)]
    [InlineData("https://example.com/test", "t", false)]
    [InlineData("https://example.com/test", "asdf", false)]
    public async Task PathFilter_StartsWith(string uri, string path, bool expectedMatch)
    {
        await ShouldMatchUriAsync(
            new Uri(uri),
            builder => builder.ForPathStartsWith(path),
            expectedMatch);
    }

    [Theory]
    [InlineData("https://example.com/?a=1&b=2", "a=1&b=2", false)]
    [InlineData("https://example.com/?a=1&b=2", "?a=1&b=2", true)]
    [InlineData("https://example.com/?a=1&b=2", "z", false)]
    public async Task QueryFilter(string uri, string query, bool expectedMatch)
    {
        await ShouldMatchUriAsync(
            new Uri(uri),
            builder => builder.ForQuery(query),
            expectedMatch);
    }

    [Theory]
    [InlineData("https://example.com/?a=1&b=2", "a=[0-9]", true)]
    [InlineData("https://example.com/?a=1&b=2", "z", false)]
    public async Task QueryFilter_Regex(string uri, string query, bool expectedMatch)
    {
        await ShouldMatchUriAsync(
            new Uri(uri),
            builder => builder.ForQuery(new Regex(query)),
            expectedMatch);
    }

    [Theory]
    [InlineData("https://example.com/?a=1&b=2", "a", true)]
    [InlineData("https://example.com/?a=1&b=2", "z", false)]
    public async Task QueryFilter_WithParameter(string uri, string parameter, bool expectedMatch)
    {
        await ShouldMatchUriAsync(
            new Uri(uri),
            builder => builder.ForQueryWithParameter(parameter),
            expectedMatch);
    }

    [Theory]
    [InlineData("https://example.com/?a=1&b=2", "a", "1", true)]
    [InlineData("https://example.com/?a=1&b=2", "a", "2", false)]
    [InlineData("https://example.com/?a=1&b=2", "z", "26", false)]
    [InlineData("https://example.com/?a=1&b=2", "z", null, true)]
    public async Task QueryFilter_WithParameterValue(string uri, string parameter, string value, bool expectedMatch)
    {
        await ShouldMatchUriAsync(
            new Uri(uri),
            builder => builder.ForQueryWithParameter(parameter, value),
            expectedMatch);
    }

    [Theory]
    [InlineData("https://example.com/?a=1&b=2", "a", "1", true)]
    [InlineData("https://example.com/?a=1&b=2", "a", "2", false)]
    [InlineData("https://example.com/?a=1&b=2", "z", "26", false)]
    public async Task QueryFilter_WithParameterValueFunc(string uri, string parameter, string value, bool expectedMatch)
    {
        await ShouldMatchUriAsync(
            new Uri(uri),
            builder => builder.ForQueryWithParameter(parameter, parameterValue => parameterValue == value),
            expectedMatch);
    }

    [Fact]
    public async Task QueryFilter_WithParameter_Overwrite_ForQuery()
    {
        await ShouldMatchUriAsync(
            new Uri("https://example.com/?a=1&b=2"),
            builder => builder
                .ForQuery("zzz")
                .ForQueryWithParameter("a"),
            true);
    }

    [Fact]
    public async Task QueryFilter_ForQuery_Overwrite_WithParameter()
    {
        await ShouldMatchUriAsync(
            new Uri("https://example.com/?a=1&b=2"),
            builder => builder
                .ForQueryWithParameter("a")
                .ForQuery("zzz"),
            false);
    }

    [Theory]
    [InlineData("https://example.com", "", true)]
    [InlineData("https://example.com/#asdf", "asdf", false)]
    [InlineData("https://example.com/#asdf", "#asdf", true)]
    [InlineData("https://example.com/#asdf", "#test", false)]
    public async Task FragmentFilter(string uri, string fragment, bool expectedMatch)
    {
        await ShouldMatchUriAsync(
            new Uri(uri),
            builder => builder.ForFragment(fragment),
            expectedMatch);
    }

    [Theory]
    [InlineData("https://example.com/#asdf", "#[a-z]", true)]
    [InlineData("https://example.com/#asdf", "#[0-9]", false)]
    public async Task FragmentFilter_Regex(string uri, string fragment, bool expectedMatch)
    {
        await ShouldMatchUriAsync(
            new Uri(uri),
            builder => builder.ForFragment(new Regex(fragment)),
            expectedMatch);
    }

    [Fact]
    public async Task RespondWith()
    {
        var statusCode = HttpStatusCode.Forbidden;
        var response = await ShouldMatchUriAsync(
            new Uri("https://example.com"),
            builder => builder.RespondWith(_ => new HttpResponseMessage(statusCode)),
            true);

        response.StatusCode.ShouldBe(statusCode);
    }

    [Fact]
    public async Task RespondWith_Async()
    {
        var statusCode = HttpStatusCode.Forbidden;
        var response = await ShouldMatchUriAsync(
            new Uri("https://example.com"),
            builder => builder.RespondWith(async (request, cancellationToken) =>
            {
                await Task.Delay(0, cancellationToken);
                return new HttpResponseMessage(statusCode);
            }),
            true);

        response.StatusCode.ShouldBe(statusCode);
    }

    [Fact]
    public async Task RespondWithStatus()
    {
        var statusCode = HttpStatusCode.Forbidden;
        var response = await ShouldMatchUriAsync(
            new Uri("https://example.com"),
            builder => builder.RespondWithStatus(statusCode),
            true);

        response.StatusCode.ShouldBe(statusCode);
    }

    [Fact]
    public async Task RespondWithContent_String()
    {
        var statusCode = HttpStatusCode.Forbidden;
        var content = "test";
        var response = await ShouldMatchUriAsync(
            new Uri("https://example.com"),
            builder => builder
                .RespondWithStatus(statusCode)
                .RespondWithContent(content),
            true);

        response.StatusCode.ShouldBe(statusCode);
        (await response.Content.ReadAsStringAsync()).ShouldBe(content);
    }

    [Fact]
    public async Task RespondWithContent_Bytes()
    {
        var statusCode = HttpStatusCode.Forbidden;
        var content = new byte[] { 1, 2, 3, 4 };
        var response = await ShouldMatchUriAsync(
            new Uri("https://example.com"),
            builder => builder
                .RespondWithStatus(statusCode)
                .RespondWithContent(content),
            true);

        response.StatusCode.ShouldBe(statusCode);
        (await response.Content.ReadAsByteArrayAsync()).ShouldBe(content);
    }

    [Fact]
    public async Task RespondWithContent_Stream()
    {
        var statusCode = HttpStatusCode.Forbidden;
        var content = Encoding.UTF8.GetBytes("test");
        var response = await ShouldMatchUriAsync(
            new Uri("https://example.com"),
            builder => builder
                .RespondWithStatus(statusCode)
                .RespondWithContent(new MemoryStream(content)),
            true);

        response.StatusCode.ShouldBe(statusCode);
        (await response.Content.ReadAsByteArrayAsync()).ShouldBe(content);
    }

    [Fact]
    public async Task RespondWithHeaders()
    {
        var content = Encoding.UTF8.GetBytes("test");
        var response = await ShouldMatchUriAsync(
            new Uri("https://example.com"),
            builder => builder
                .RespondWithStatus(HttpStatusCode.Forbidden)
                .RespondWithHeader("test", "abc")
                .RespondWithHeader("asdf", new[] { "abc", "def" }),
            true);

        response.Headers.GetValues("test").ShouldBe(new[] { "abc" });
        response.Headers.GetValues("asdf").ShouldBe(new[] { "abc", "def" });
    }

    [Fact]
    public async Task RespondWithHeaders_HeadersOnly_NotMatch()
    {
        await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            var response = await ShouldMatchUriAsync(
                new Uri("https://example.com"),
                builder => builder
                    .RespondWithHeader("test", "abc"),
                true);
        });
    }

    [Fact]
    public async Task RequestFilter()
    {
        await ShouldMatchUriAsync(
            new Uri("https://example.com"),
            builder => builder.ForRequest(_ => true),
            true);
    }

    [Fact]
    public async Task ModifyRequest()
    {
        var response = await ShouldMatchUriAsync(
            new Uri("https://example.com"),
            builder => builder.ModifyRequestWith(request =>
            {
                request.Headers.Add("test", "asdf");
                return request;
            }),
            true);

        response.RequestMessage!.Headers.GetValues("test").Where(v => v == "asdf").Any().ShouldBeTrue();
    }

    [Fact]
    public async Task ModifyResponse()
    {
        var response = await ShouldMatchUriAsync(
            new Uri("https://example.com"),
            builder => builder.ModifyResponseWith(response =>
            {
                response.Headers.Add("test", "asdf");
                return response;
            }),
            true);

        response.Headers.GetValues("test").Where(v => v == "asdf").Any().ShouldBeTrue();
    }

    private async Task ShouldMatchVersionAsync(Version version, Action<HttpClientInterceptionRuleBuilder> action, bool expectedMatch)
    {
        await ShouldMatchRequestAsync(
            new HttpRequestMessage(HttpMethod.Get, "https://example.com")
            {
                Version = version
            },
            action,
            expectedMatch);
    }

    private async Task ShouldMatchMethodAsync(HttpMethod method, Action<HttpClientInterceptionRuleBuilder> action, bool expectedMatch)
    {
        await ShouldMatchRequestAsync(new HttpRequestMessage(method, "https://example.com"), action, expectedMatch);
    }

    private async Task<HttpResponseMessage> ShouldMatchUriAsync(Uri uri, Action<HttpClientInterceptionRuleBuilder> action, bool expectedMatch)
    {
        return await ShouldMatchRequestAsync(new HttpRequestMessage(HttpMethod.Get, uri), action, expectedMatch);
    }

    private async Task<HttpResponseMessage> ShouldMatchRequestAsync(HttpRequestMessage request, Action<HttpClientInterceptionRuleBuilder> action, bool expectedMatch)
    {
        var builder = new HttpClientInterceptionRuleBuilder();
        action(builder);
        builder.InvokeEvents();

        var match = false;
        _interceptor.AddRule(builder.Create());
        _interceptor.RequestProcessed += (sender, request) => match = true;
        var client = new HttpClient(_interceptor);

        var response = await client.SendAsync(request);

        match.ShouldBe(expectedMatch);
        return response;
    }
}
