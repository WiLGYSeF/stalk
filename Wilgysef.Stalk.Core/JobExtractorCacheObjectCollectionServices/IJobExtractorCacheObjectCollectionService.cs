namespace Wilgysef.Stalk.Core.JobExtractorCacheObjectCollectionServices;

public interface IJobExtractorCacheObjectCollectionService
{
    IExtractorCacheObjectCollection GetCacheCollection(long jobId);

    bool RemoveCacheCollection(long jobId);
}
