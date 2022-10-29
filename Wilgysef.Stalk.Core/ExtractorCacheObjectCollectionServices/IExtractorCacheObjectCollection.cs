using Wilgysef.Stalk.Core.Shared.CacheObjects;
using Wilgysef.Stalk.Core.Shared.Extractors;

namespace Wilgysef.Stalk.Core.ExtractorCacheObjectCollectionServices;

public interface IExtractorCacheObjectCollectionService
{
    /// <summary>
    /// Gets the extractor cache object.
    /// </summary>
    /// <param name="extractor">Extractor.</param>
    /// <returns>Cache.</returns>
    ICacheObject<string, object?> GetCache(IExtractor extractor);
}
