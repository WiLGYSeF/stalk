using System.Collections.Concurrent;
using Wilgysef.Stalk.Core.CacheObjects;
using Wilgysef.Stalk.Core.Shared.CacheObjects;
using Wilgysef.Stalk.Core.Shared.Extractors;

namespace Wilgysef.Stalk.Core.ExtractorCacheObjectCollectionServices;

public class ExtractorCacheObjectCollection : IExtractorCacheObjectCollection
{
    private readonly ConcurrentDictionary<Type, ICacheObject<string, object?>> _caches = new();

    public ICacheObject<string, object?> GetCache(IExtractor extractor)
    {
        var extractorType = extractor.GetType();

        if (!_caches.TryGetValue(extractorType, out var cache))
        {
            cache = new CacheObject<string, object?>();
            _caches[extractorType] = cache;
        }
        return cache;
    }
}
