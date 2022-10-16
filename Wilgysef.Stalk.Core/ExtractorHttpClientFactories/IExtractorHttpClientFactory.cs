namespace Wilgysef.Stalk.Core.ExtractorHttpClientFactories;

public interface IExtractorHttpClientFactory
{
    /// <summary>
    /// Creates an HttpClient for the extractor.
    /// </summary>
    /// <param name="extractorConfig">Extractor config.</param>
    /// <returns>HTTP client.</returns>
    HttpClient CreateClient(IDictionary<string, object?> extractorConfig);
}
