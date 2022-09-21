namespace Wilgysef.Stalk.Core.ExtractorCacheObjectCollectionServices;

public interface IJobExtractorCacheObjectCollectionService
{
    IExtractorCacheObjectCollection GetCacheCollection(long jobId);

    bool RemoveCacheCollection(long jobId);
}
