using Wilgysef.Stalk.Core.Shared.Dependencies;
using Wilgysef.Stalk.Core.Shared.Extractors;

namespace Wilgysef.Stalk.Core.ExtractorHttpClientFactories;

public class ExtractorHttpClientService : IExtractorHttpClientService, ITransientDependency
{
    private readonly IExtractorHttpClientFactory _extractorHttpClientFactory;
    private readonly IExtractorHttpClientCollectionService _extractorHttpClientCollectionServiceInternal;

    public ExtractorHttpClientService(
        IExtractorHttpClientFactory extractorHttpClientFactory,
        IExtractorHttpClientCollectionService extractorHttpClientCollectionServiceInternal)
    {
        _extractorHttpClientFactory = extractorHttpClientFactory;
        _extractorHttpClientCollectionServiceInternal = extractorHttpClientCollectionServiceInternal;
    }

    public HttpClient GetHttpClient(IExtractor extractor, IDictionary<string, object?> extractorConfig)
    {
        return _extractorHttpClientCollectionServiceInternal.GetHttpClient(
            extractor,
            _extractorHttpClientFactory,
            extractorConfig);
    }
}
