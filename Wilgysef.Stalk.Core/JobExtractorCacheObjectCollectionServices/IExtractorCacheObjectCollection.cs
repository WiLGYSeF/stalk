using Wilgysef.Stalk.Core.Shared.CacheObjects;
using Wilgysef.Stalk.Core.Shared.Extractors;

namespace Wilgysef.Stalk.Core.JobExtractorCacheObjectCollectionServices;

public interface IExtractorCacheObjectCollection
{
    ICacheObject<string, object?> GetCache(IExtractor extractor);
}
