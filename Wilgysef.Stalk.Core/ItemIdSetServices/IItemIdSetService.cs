namespace Wilgysef.Stalk.Core.ItemIdSetServices;

public interface IItemIdSetService
{
    /// <summary>
    /// Gets an item Id set.
    /// </summary>
    /// <param name="path">Path.</param>
    /// <param name="jobId">Job Id.</param>
    /// <returns>Item Ids.</returns>
    Task<IItemIdSet> GetItemIdSetAsync(string path, long jobId);

    /// <summary>
    /// Removes all item Id sets with job Id.
    /// </summary>
    /// <param name="jobId">Job Id.</param>
    /// <returns><see langword="true"/> if the item Id set was removed, otherwise <see langword="false"/>.</returns>
    Task<bool> RemoveItemIdSetAsync(long jobId);

    /// <summary>
    /// Removes an item Id set.
    /// </summary>
    /// <param name="path">Path.</param>
    /// <param name="jobId">Job Id.</param>
    /// <returns><see langword="true"/> if the item Id set was removed, otherwise <see langword="false"/>.</returns>
    Task<bool> RemoveItemIdSetAsync(string path, long jobId);

    /// <summary>
    /// Write item Id set changes.
    /// </summary>
    /// <param name="path">Path.</param>
    /// <param name="itemIds">Item Ids.</param>
    /// <returns>Number of item Ids written.</returns>
    Task<int> WriteChangesAsync(string path, IItemIdSet itemIds);
}
