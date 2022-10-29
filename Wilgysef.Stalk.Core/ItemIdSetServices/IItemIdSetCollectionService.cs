namespace Wilgysef.Stalk.Core.ItemIdSetServices;

public interface IItemIdSetCollectionService
{
    /// <summary>
    /// Adds an item Id set.
    /// </summary>
    /// <param name="path">Path.</param>
    /// <param name="jobId">Job Id.</param>
    /// <param name="itemIds">Item Ids.</param>
    void AddItemIdSet(string path, long jobId, IItemIdSet itemIds);

    /// <summary>
    /// Gets an item Id set.
    /// </summary>
    /// <param name="path">Item Id set file path.</param>
    /// <param name="jobId">Job Id.</param>
    /// <returns>Item Id set.</returns>
    IItemIdSet? GetItemIdSet(string path, long jobId);

    /// <summary>
    /// Removes all item Id sets with job Id.
    /// </summary>
    /// <param name="jobId">Job Id.</param>
    /// <returns><see langword="true"/> if an item was removed, otherwise <see langword="false"/>.</returns>
    bool RemoveItemIdSet(long jobId);

    /// <summary>
    /// Removes an item Id set.
    /// </summary>
    /// <param name="path">Path.</param>
    /// <param name="jobId">Job Id.</param>
    /// <returns><see langword="true"/> if an item was removed, otherwise <see langword="false"/>.</returns>
    bool RemoveItemIdSet(string path, long jobId);
}
