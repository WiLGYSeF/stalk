namespace Wilgysef.Stalk.Core.ItemIdSetServices;

public class ItemIdSet : IItemIdSet
{
    public int Count => _itemIds.Count;

    public IReadOnlyCollection<string> Items => _itemIds;

    public IReadOnlyCollection<string> PendingItems => _pendingItemIds;

    private readonly HashSet<string> _itemIds = new();

    private readonly HashSet<string> _pendingItemIds = new();

    private readonly object _lock = new();

    public ItemIdSet() { }

    public ItemIdSet(IEnumerable<string> items)
    {
        foreach (var item in items)
        {
            _itemIds.Add(item);
        }
    }

    public bool Add(string item)
    {
        lock (_lock)
        {
            _pendingItemIds.Add(item);
            return _itemIds.Add(item);
        }
    }

    public bool AddNoPending(string item)
    {
        lock (_lock)
        {
            return _itemIds.Add(item);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _pendingItemIds.Clear();
            _itemIds.Clear();
        }
    }

    public bool Contains(string item)
    {
        lock (_lock)
        {
            return _itemIds.Contains(item);
        }
    }

    public bool Remove(string item)
    {
        lock (_lock)
        {
            _pendingItemIds.Remove(item);
            return _itemIds.Remove(item);
        }
    }

    public int ResetChangeTracking()
    {
        lock (_lock)
        {
            var count = _pendingItemIds.Count;
            _pendingItemIds.Clear();
            return count;
        }
    }
}
