using System.Diagnostics.CodeAnalysis;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.ItemIdSetServices;

public interface IItemSetCollectionService : ISingletonDependency
{
    /// <summary>
    /// Adds an item Id set.
    /// </summary>
    /// <param name="path">Path.</param>
    /// <param name="itemIds">Item Ids.</param>
    void AddItemIdSet(string path, IItemIdSet itemIds);

    /// <summary>
    /// Gets an item Id set.
    /// </summary>
    /// <param name="path">Item Id set file path.</param>
    /// <returns>Item Id set.</returns>
    /// <exception cref="KeyNotFoundException">The item Id was not found.</exception>
    IItemIdSet GetItemIdSet(string path);

    /// <summary>
    /// Tries to get an item Id set.
    /// </summary>
    /// <param name="path">Path.</param>
    /// <param name="itemIds">Item Ids.</param>
    /// <returns><see langword="true"/> if an item was found, otherwise <see langword="false"/>.</returns>
    bool TryGetItemIdSet(string path, [MaybeNullWhen(false)] out IItemIdSet itemIds);

    /// <summary>
    /// Removes an item Id set.
    /// </summary>
    /// <param name="path">Path.</param>
    /// <returns><see langword="true"/> if an item was removed, otherwise <see langword="false"/>.</returns>
    bool RemoveItemIdSet(string path);
}
