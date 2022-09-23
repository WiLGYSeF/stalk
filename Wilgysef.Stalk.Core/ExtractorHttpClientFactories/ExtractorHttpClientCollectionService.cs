using System.Collections.Concurrent;
using Wilgysef.Stalk.Core.Shared.Dependencies;
using Wilgysef.Stalk.Core.Shared.Extractors;

namespace Wilgysef.Stalk.Core.ExtractorHttpClientFactories;

public class ExtractorHttpClientCollectionService : IExtractorHttpClientCollectionService, IScopedDependency
{
    private readonly ConcurrentDictionary<Type, HttpClient> _clients = new();

    public HttpClient GetHttpClient(
        IExtractor extractor,
        IExtractorHttpClientFactory extractorHttpClientFactory,
        IDictionary<string, object?> extractorConfig)
    {
        var type = extractor.GetType();
        if (!_clients.TryGetValue(type, out var client))
        {
            client = extractorHttpClientFactory.CreateClient(extractorConfig);
            _clients[type] = client;
        }
        return client;
    }
}
