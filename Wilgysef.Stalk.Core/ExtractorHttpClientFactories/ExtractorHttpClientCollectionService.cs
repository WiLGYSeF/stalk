using Wilgysef.Stalk.Core.Shared.Dependencies;
using Wilgysef.Stalk.Core.Shared.Extractors;

namespace Wilgysef.Stalk.Core.ExtractorHttpClientFactories;

public class ExtractorHttpClientCollectionService : IExtractorHttpClientCollectionService, ITransientDependency
{
    private readonly IExtractorHttpClientFactory _extractorHttpClientFactory;
    private readonly IExtractorHttpClientCollectionServiceInternal _extractorHttpClientCollectionServiceInternal;

    public ExtractorHttpClientCollectionService(
        IExtractorHttpClientFactory extractorHttpClientFactory,
        IExtractorHttpClientCollectionServiceInternal extractorHttpClientCollectionServiceInternal)
    {
        _extractorHttpClientFactory = extractorHttpClientFactory;
        _extractorHttpClientCollectionServiceInternal = extractorHttpClientCollectionServiceInternal;
    }

    public HttpClient GetHttpClient(long jobId, IExtractor extractor, IDictionary<string, object?> extractorConfig)
    {
        return _extractorHttpClientCollectionServiceInternal.GetHttpClient(
            jobId,
            extractor,
            _extractorHttpClientFactory,
            extractorConfig);
    }

    public bool RemoveHttpClients(long jobId)
    {
        return _extractorHttpClientCollectionServiceInternal.RemoveHttpClients(jobId);
    }
}
