using Wilgysef.Stalk.Core.Shared.Extractors;

namespace Wilgysef.Stalk.Core.ExtractorHttpClientFactories;

public interface IExtractorHttpClientCollectionService
{
    /// <summary>
    /// Gets an <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="extractor">Extractor.</param>
    /// <param name="extractorHttpClientFactory">Extractor HTTP client factory.</param>
    /// <param name="extractorConfig">Extractor config, used if no existing HTTP client exists.</param>
    /// <returns><see cref="HttpClient"/>.</returns>
    HttpClient GetHttpClient(
        IExtractor extractor,
        IExtractorHttpClientFactory extractorHttpClientFactory,
        IDictionary<string, object?> extractorConfig);
}
