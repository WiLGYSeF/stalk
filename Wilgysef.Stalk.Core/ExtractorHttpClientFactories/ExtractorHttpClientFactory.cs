﻿using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Dependencies;
using Wilgysef.Stalk.Core.UserAgentGenerators;

namespace Wilgysef.Stalk.Core.ExtractorHttpClientFactories;

public class ExtractorHttpClientFactory : IExtractorHttpClientFactory, ITransientDependency
{
    private readonly IUserAgentGenerator _userAgentGenerator;
    private readonly HttpClient _httpClient;

    public ExtractorHttpClientFactory(
        IUserAgentGenerator userAgentGenerator,
        HttpClient httpClient)
    {
        _userAgentGenerator = userAgentGenerator;
        _httpClient = httpClient;
    }

    public HttpClient CreateClient(IDictionary<string, object?> extractorConfig)
    {
        ConfigureClient(_httpClient, extractorConfig);
        return _httpClient;
    }

    private void ConfigureClient(HttpClient client, IDictionary<string, object?> extractorConfig)
    {
        if (extractorConfig.TryGetValue(JobConfig.ExtractorConfigKeys.UserAgent, out var userAgent))
        {
            SetUserAgentHeader(client, userAgent?.ToString());
        }
        else
        {
            SetUserAgentHeader(client, _userAgentGenerator.Create());
        }
    }

    private static bool SetUserAgentHeader(HttpClient client, string? value)
    {
        if (client.DefaultRequestHeaders.UserAgent.Count > 0)
        {
            return false;
        }

        client.DefaultRequestHeaders.Add("User-Agent", value);
        return true;
    }
}
