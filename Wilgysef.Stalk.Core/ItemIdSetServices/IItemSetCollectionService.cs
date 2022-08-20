using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.ItemIdSetServices;

public interface IItemSetCollectionService : ISingletonDependency
{
    /// <summary>
    /// Gets an item Id set.
    /// </summary>
    /// <param name="path">Item Id set file path.</param>
    /// <returns>Item Id set.</returns>
    IItemIdSet GetItemIdSet(string path);
}
