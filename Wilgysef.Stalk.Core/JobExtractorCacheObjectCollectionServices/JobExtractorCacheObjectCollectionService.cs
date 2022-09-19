using System.Collections.Concurrent;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.JobExtractorCacheObjectCollectionServices;

public class JobExtractorCacheObjectCollectionService : IJobExtractorCacheObjectCollectionService, ISingletonDependency
{
    private ConcurrentDictionary<long, IExtractorCacheObjectCollection> _cacheCollections = new();

    public IExtractorCacheObjectCollection GetCacheCollection(long jobId)
    {
        if (!_cacheCollections.TryGetValue(jobId, out var cacheCollection))
        {
            cacheCollection = new ExtractorCacheObjectCollection();
            _cacheCollections[jobId] = cacheCollection;
        }
        return cacheCollection;
    }

    public bool RemoveCacheCollection(long jobId)
    {
        return _cacheCollections.Remove(jobId, out _);
    }
}
