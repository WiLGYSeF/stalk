using Wilgysef.Stalk.Core.Shared.Extractors;

namespace Wilgysef.Stalk.Core.ExtractorHttpClientFactories;

public interface IExtractorHttpClientService
{
    /// <summary>
    /// Gets or creates an <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="extractor">Extractor.</param>
    /// <param name="extractorConfig">Extractor config, used if no existing HTTP client exists.</param>
    /// <returns><see cref="HttpClient"/>.</returns>
    HttpClient GetHttpClient(IExtractor extractor, IDictionary<string, object?> extractorConfig);
}
