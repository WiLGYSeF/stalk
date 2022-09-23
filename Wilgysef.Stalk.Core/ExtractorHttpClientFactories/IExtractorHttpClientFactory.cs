namespace Wilgysef.Stalk.Core.ExtractorHttpClientFactories;

public interface IExtractorHttpClientFactory
{
    HttpClient CreateClient(IDictionary<string, object?> extractorConfig);
}
