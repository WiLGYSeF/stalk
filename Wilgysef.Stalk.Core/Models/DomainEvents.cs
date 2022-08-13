using System.Collections;
using Wilgysef.Stalk.Core.DomainEvents;

namespace Wilgysef.Stalk.Core.Models;

public class DomainEventCollection : ICollection<IDomainEvent>
{
    public int Count => _domainEvents.Count;

    public bool IsReadOnly => false;

    private readonly List<IDomainEvent> _domainEvents = new();

    public void Add(IDomainEvent item)
    {
        _domainEvents.Add(item);
    }

    public bool AddOrReplace(IDomainEvent item)
    {
        var itemType = item.GetType();

        for (var i = 0; i < _domainEvents.Count; i++)
        {
            if (_domainEvents[i].GetType() == itemType)
            {
                _domainEvents[i] = item;
                return true;
            }
        }

        Add(item);
        return false;
    }

    public bool Remove(IDomainEvent item)
    {
        return _domainEvents.Remove(item);
    }

    public int RemoveType<T>() where T : IDomainEvent
    {
        return RemoveType(typeof(T));
    }

    public int RemoveType(Type type)
    {
        var toRemove = _domainEvents.Where(e => e.GetType() == type).ToList();

        foreach (var item in toRemove)
        {
            _domainEvents.Remove(item);
        }

        return toRemove.Count;
    }

    public void Clear()
    {
        _domainEvents.Clear();
    }

    public bool Contains(IDomainEvent item)
    {
        return _domainEvents.Contains(item);
    }

    public bool ContainsType<T>() where T : IDomainEvent
    {
        return ContainsType(typeof(T));
    }

    public bool ContainsType(Type type)
    {
        return _domainEvents.Any(e => e.GetType() == type);
    }

    public void CopyTo(IDomainEvent[] array, int arrayIndex)
    {
        _domainEvents.CopyTo(array, arrayIndex);
    }

    public IEnumerator<IDomainEvent> GetEnumerator()
    {
        return _domainEvents.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _domainEvents.GetEnumerator();
    }
}
