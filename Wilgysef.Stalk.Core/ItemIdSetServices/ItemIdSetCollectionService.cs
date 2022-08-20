namespace Wilgysef.Stalk.Core.ItemIdSetServices;

public class ItemIdSetCollectionService : IItemSetCollectionService
{
    private readonly Dictionary<string, IItemIdSet> _itemIdSets = new();

    public IItemIdSet GetItemIdSet(string path)
    {
        if (!_itemIdSets.TryGetValue(path, out var itemIds))
        {

        }
        return itemIds;
    }
}
