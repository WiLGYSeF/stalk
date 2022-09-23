using Wilgysef.Stalk.Core.Shared.CacheObjects;
using Wilgysef.Stalk.Core.Shared.Extractors;

namespace Wilgysef.Stalk.Core.ExtractorCacheObjectCollectionServices;

public interface IExtractorCacheObjectCollectionService
{
    ICacheObject<string, object?> GetCache(IExtractor extractor);
}
