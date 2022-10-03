using Shouldly;
using System.Net;

namespace Wilgysef.HttpClientInterception.Tests;

public class HttpClientInterceptorTest
{
    private readonly HttpClientInterceptor _interceptor;

    public HttpClientInterceptorTest()
    {
        _interceptor = HttpClientInterceptor.Create()
            .AddForAny(request => new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                RequestMessage = request
            });
    }

    [Fact]
    public async Task SendResponse_Multiple()
    {
        _interceptor
            .AddRule(new HttpClientInterceptionRuleBuilder()
                .RespondWithContent("test")
                .Create())
            .AddRule(new HttpClientInterceptionRuleBuilder()
                .RespondWithContent("asdf")
                .Create());
        var client = new HttpClient(_interceptor);

        var response = await client.GetAsync("https://example.com");

        (await response.Content.ReadAsStringAsync()).ShouldBe("asdf");
    }

    [Fact]
    public async Task ModifyRequest_Multiple()
    {
        _interceptor
            .AddRule(new HttpClientInterceptionRuleBuilder()
                .ModifyRequestWith(request =>
                {
                    request.Headers.Add("test", "asdf");
                    return request;
                })
                .Create())
            .AddRule(new HttpClientInterceptionRuleBuilder()
                .ModifyRequestWith(request =>
                {
                    request.Headers.Add("abc", "test");
                    return request;
                })
                .Create());
        var client = new HttpClient(_interceptor);

        var response = await client.GetAsync("https://example.com");

        response.RequestMessage!.Headers.GetValues("test").Where(v => v == "asdf").Any().ShouldBeTrue();
        response.RequestMessage!.Headers.GetValues("abc").Where(v => v == "test").Any().ShouldBeTrue();
    }

    [Fact]
    public async Task ModifyResponse_Multiple()
    {
        _interceptor
            .AddRule(new HttpClientInterceptionRuleBuilder()
                .ModifyResponseWith(response =>
                {
                    response.Headers.Add("test", "asdf");
                    return response;
                })
                .Create())
            .AddRule(new HttpClientInterceptionRuleBuilder()
                .ModifyResponseWith(response =>
                {
                    response.Headers.Add("abc", "test");
                    return response;
                })
                .Create());
        var client = new HttpClient(_interceptor);

        var response = await client.GetAsync("https://example.com");

        response.Headers.GetValues("test").Where(v => v == "asdf").Any().ShouldBeTrue();
        response.Headers.GetValues("abc").Where(v => v == "test").Any().ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Match_Catch_Exception(bool throws)
    {
        var threw = false;
        _interceptor.ThrowOnError = throws;
        _interceptor.ErrorOccurred += (sender, exception) => threw = true;

        _interceptor
            .AddRule(new HttpClientInterceptionRuleBuilder()
                .ForRequest(_ => throw new InvalidOperationException())
                .Create());
        var client = new HttpClient(_interceptor);

        if (throws)
        {
            await Should.ThrowAsync<InvalidOperationException>(async () => await client.GetAsync("https://example.com"));
        }
        else
        {
            var response = await client.GetAsync("https://example.com");
            threw.ShouldBeTrue();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ModiifyResponse_Catch_Exception(bool throws)
    {
        var threw = false;
        _interceptor.ThrowOnError = throws;
        _interceptor.ErrorOccurred += (sender, exception) => threw = true;

        _interceptor
            .AddRule(new HttpClientInterceptionRuleBuilder()
                .ModifyResponseWith(_ => throw new InvalidOperationException())
                .Create());
        var client = new HttpClient(_interceptor);

        if (throws)
        {
            await Should.ThrowAsync<InvalidOperationException>(async () => await client.GetAsync("https://example.com"));
        }
        else
        {
            var response = await client.GetAsync("https://example.com");
            threw.ShouldBeTrue();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LogRequest_Catch_Exception(bool throws)
    {
        var threw = false;
        _interceptor.ThrowOnError = throws;
        _interceptor.ErrorOccurred += (sender, exception) => threw = true;

        _interceptor
            .AddRule(new HttpClientInterceptionRuleBuilder().InvokeRequestMessageEvents().Create());
        _interceptor.RequestProcessed += (sender, request) => throw new InvalidOperationException();
        var client = new HttpClient(_interceptor);

        if (throws)
        {
            await Should.ThrowAsync<InvalidOperationException>(async () => await client.GetAsync("https://example.com"));
        }
        else
        {
            var response = await client.GetAsync("https://example.com");
            threw.ShouldBeTrue();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LogResponse_Catch_Exception(bool throws)
    {
        var threw = false;
        _interceptor.ThrowOnError = throws;
        _interceptor.ErrorOccurred += (sender, exception) => threw = true;

        _interceptor
            .AddRule(new HttpClientInterceptionRuleBuilder().InvokeResponseMessageEvents().Create());
        _interceptor.ResponseReceived += (sender, response) => throw new InvalidOperationException();
        var client = new HttpClient(_interceptor);

        if (throws)
        {
            await Should.ThrowAsync<InvalidOperationException>(async () => await client.GetAsync("https://example.com"));
        }
        else
        {
            var response = await client.GetAsync("https://example.com");
            threw.ShouldBeTrue();
        }
    }
}
