using Polly.Extensions.Http;
using Polly;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;

namespace Wilgysef.Stalk.Application.HttpClientPolicies;

public static class HttpClientPolicy
{
    public static IHttpClientBuilder AddHttpClientPolicy(this IHttpClientBuilder builder)
    {
        builder.AddPolicyHandler(GetRetryPolicy());
        return builder;
    }

    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromMilliseconds(5000 + RandomInt(-500, 500)));
    }

    private static int RandomInt(int min, int max)
    {
        return RandomNumberGenerator.GetInt32(min, max);
    }
}
