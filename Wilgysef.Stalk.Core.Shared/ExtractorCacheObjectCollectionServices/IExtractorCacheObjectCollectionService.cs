using Wilgysef.Stalk.Core.Shared.CacheObjects;
using Wilgysef.Stalk.Core.Shared.Extractors;

namespace Wilgysef.Stalk.Core.Shared.ExtractorCacheObjectCollectionServices
{
    public interface IExtractorCacheObjectCollectionService
    {
        ICacheObject GetCache(IExtractor extractor);
    }
}
