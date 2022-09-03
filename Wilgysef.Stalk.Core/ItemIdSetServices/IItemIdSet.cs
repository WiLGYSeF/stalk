namespace Wilgysef.Stalk.Core.ItemIdSetServices;

public interface IItemIdSet
{
    /// <summary>
    /// Item Id count.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Item Ids.
    /// </summary>
    IReadOnlyCollection<string> Items { get; }

    /// <summary>
    /// Pending items that have not been written.
    /// </summary>
    IReadOnlyCollection<string> PendingItems { get; }

    /// <summary>
    /// Adds item Id and adds it to the pending items.
    /// </summary>
    /// <param name="item">Item Id.</param>
    /// <returns><see langword="true"/> if the item was added, otherwise <see langword="false"/>.</returns>
    bool Add(string item);

    /// <summary>
    /// Adds item Id without adding it to the pending items.
    /// </summary>
    /// <param name="item">Item Id.</param>
    /// <returns><see langword="true"/> if the item was added, otherwise <see langword="false"/>.</returns>
    bool AddNoPending(string item);

    /// <summary>
    /// Clears the item Ids.
    /// </summary>
    void Clear();

    /// <summary>
    /// Checks if the item Ids contain an Id.
    /// </summary>
    /// <param name="item">Item Id.</param>
    /// <returns><see langword="true"/> if the item exists, otherwise <see langword="false"/>.</returns>
    bool Contains(string item);

    /// <summary>
    /// Removes item Id.
    /// </summary>
    /// <param name="item">Item Id.</param>
    /// <returns><see langword="true"/> if the item existed, otherwise <see langword="false"/>.</returns>
    bool Remove(string item);

    /// <summary>
    /// Remove pending items.
    /// </summary>
    /// <returns></returns>
    int ResetChangeTracking();
}
