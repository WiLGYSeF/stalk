using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.ItemIdSetServices;

public class ItemIdSetCollectionService : IItemSetCollectionService, ISingletonDependency
{
    private readonly ConcurrentDictionary<string, IItemIdSet> _itemIdSets = new();

    public void AddItemIdSet(string path, IItemIdSet itemIds)
    {
        _itemIdSets[path] = itemIds;
    }

    public IItemIdSet GetItemIdSet(string path)
    {
        return _itemIdSets[path];
    }

    public bool TryGetItemIdSet(string path, [MaybeNullWhen(false)] out IItemIdSet itemIds)
    {
        return _itemIdSets.TryGetValue(path, out itemIds);
    }

    public bool RemoveItemIdSet(string path)
    {
        return _itemIdSets.Remove(path, out _);
    }
}
