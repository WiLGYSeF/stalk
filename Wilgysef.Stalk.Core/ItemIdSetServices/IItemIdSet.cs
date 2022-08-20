namespace Wilgysef.Stalk.Core.ItemIdSetServices;

public interface IItemIdSet
{
    int Count { get; }

    IReadOnlyCollection<string> PendingItems { get; }

    bool Add(string item);

    void Clear();

    bool Contains(string item);

    bool Remove(string item);

    int ResetChangeTracking();
}
