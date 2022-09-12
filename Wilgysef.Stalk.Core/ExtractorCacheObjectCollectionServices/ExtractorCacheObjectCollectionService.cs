using System.Collections.Concurrent;
using Wilgysef.Stalk.Core.CacheObjects;
using Wilgysef.Stalk.Core.Shared.CacheObjects;
using Wilgysef.Stalk.Core.Shared.Dependencies;
using Wilgysef.Stalk.Core.Shared.ExtractorCacheObjectCollectionServices;
using Wilgysef.Stalk.Core.Shared.Extractors;

namespace Wilgysef.Stalk.Core.ExtractorCacheObjectCollectionServices;

public class ExtractorCacheObjectCollectionService : IExtractorCacheObjectCollectionService, ISingletonDependency
{
    private ConcurrentDictionary<Type, ICacheObject> _caches = new();

    public ICacheObject GetCache(IExtractor extractor)
    {
        var extractorType = extractor.GetType();

        if (!_caches.TryGetValue(extractorType, out var cache))
        {
            cache = new CacheObject();
            _caches[extractorType] = cache;
        }
        return cache;
    }
}
