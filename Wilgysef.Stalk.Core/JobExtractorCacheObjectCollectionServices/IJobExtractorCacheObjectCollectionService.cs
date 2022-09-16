namespace Wilgysef.Stalk.Core.JobExtractorCacheObjectCollectionServices;

public interface IJobExtractorCacheObjectCollectionService
{
    IExtractorCacheObjectCollection GetCacheCollection(long jobId);
}
