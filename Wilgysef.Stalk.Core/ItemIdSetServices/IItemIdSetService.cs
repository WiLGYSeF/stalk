using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.ItemIdSetServices;

public interface IItemIdSetService : ITransientDependency
{
    /// <summary>
    /// Gets an item Id set.
    /// </summary>
    /// <param name="path">Path.</param>
    /// <returns>Item Ids.</returns>
    Task<IItemIdSet> GetItemIdSetAsync(string path);

    /// <summary>
    /// Write item Id set changes.
    /// </summary>
    /// <param name="path">Path.</param>
    /// <param name="itemIds">Item Ids.</param>
    /// <returns>Number of item Ids written.</returns>
    Task<int> WriteChangesAsync(string path, IItemIdSet itemIds);
}
