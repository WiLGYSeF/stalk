using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.ItemIdSetServices;

public class ItemIdSetCollectionService : IItemIdSetCollectionService, ISingletonDependency
{
    private readonly Dictionary<string, ItemIdSetEntry> _itemIdSets = new();
    private readonly object _lock = new();

    public void AddItemIdSet(string path, long jobId, IItemIdSet itemIds)
    {
        lock (_lock)
        {
            if (!_itemIdSets.TryGetValue(path, out var entry))
            {
                entry = new ItemIdSetEntry(itemIds);
                _itemIdSets[path] = entry;
            }

            entry.JobIds.Add(jobId);
        }
    }

    public IItemIdSet? GetItemIdSet(string path, long jobId)
    {
        lock (_lock)
        {
            if (!_itemIdSets.TryGetValue(path, out var entry))
            {
                return null;
            }

            entry.JobIds.Add(jobId);
            return entry.ItemIds;
        }
    }

    public bool RemoveItemIdSet(long jobId)
    {
        lock (_lock)
        {
            var removePaths = new List<string>();
            var success = false;

            foreach (var (path, entry) in _itemIdSets)
            {
                success |= entry.JobIds.Remove(jobId);
                if (entry.JobIds.Count == 0)
                {
                    removePaths.Add(path);
                }
            }

            foreach (var path in removePaths)
            {
                _itemIdSets.Remove(path, out _);
            }
            return success;
        }
    }

    public bool RemoveItemIdSet(string path, long jobId)
    {
        lock (_lock)
        {
            if (!_itemIdSets.TryGetValue(path, out var entry))
            {
                return false;
            }

            entry.JobIds.Remove(jobId);
            if (entry.JobIds.Count == 0)
            {
                _itemIdSets.Remove(path, out _);
            }
            return true;
        }
    }

    private class ItemIdSetEntry
    {
        public IItemIdSet ItemIds { get; }

        public HashSet<long> JobIds { get; }

        public ItemIdSetEntry(IItemIdSet itemIds)
        {
            ItemIds = itemIds;
            JobIds = new();
        }
    }
}
