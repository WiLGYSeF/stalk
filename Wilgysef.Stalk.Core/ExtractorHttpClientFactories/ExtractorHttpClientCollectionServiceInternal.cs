using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Wilgysef.Stalk.Core.Shared.Dependencies;
using Wilgysef.Stalk.Core.Shared.Extractors;

namespace Wilgysef.Stalk.Core.ExtractorHttpClientFactories;

public class ExtractorHttpClientCollectionServiceInternal : IExtractorHttpClientCollectionServiceInternal, ISingletonDependency
{
    private readonly ConcurrentDictionary<long, ExtractorHttpClientCollection> _clientCollections = new();

    public HttpClient GetHttpClient(
        long jobId,
        IExtractor extractor,
        IExtractorHttpClientFactory extractorHttpClientFactory,
        IDictionary<string, object?> extractorConfig)
    {
        if (!_clientCollections.TryGetValue(jobId, out var collection))
        {
            collection = new ExtractorHttpClientCollection();
            _clientCollections[jobId] = collection;
        }
        if (!collection.TryGetHttpClient(extractor, out var client))
        {
            client = extractorHttpClientFactory.CreateClient(extractorConfig);
            collection.SetHttpClient(extractor, client);
        }
        return client;
    }

    public bool RemoveHttpClients(long jobId)
    {
        return _clientCollections.Remove(jobId, out _);
    }

    private class ExtractorHttpClientCollection
    {
        private readonly ConcurrentDictionary<Type, HttpClient> _clients = new();

        public void SetHttpClient(IExtractor extractor, HttpClient client)
        {
            _clients[extractor.GetType()] = client;
        }

        public bool TryGetHttpClient(IExtractor extractor, [MaybeNullWhen(false)] out HttpClient client)
        {
            return _clients.TryGetValue(extractor.GetType(), out client);
        }
    }
}
