using System.Collections;
using Wilgysef.Stalk.Core.DomainEvents;

namespace Wilgysef.Stalk.Core.Models;

public class DomainEventCollection : ICollection<IDomainEvent>
{
    /// <summary>
    /// Number of domain events.
    /// </summary>
    public int Count => _domainEvents.Count;

    public bool IsReadOnly => false;

    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// Adds a domain event.
    /// </summary>
    /// <param name="item">Domain event.</param>
    public void Add(IDomainEvent item)
    {
        _domainEvents.Add(item);
    }

    /// <summary>
    /// Adds a domain event.
    /// If any domain events with the same type have already been added, remove them.
    /// </summary>
    /// <param name="item">Domain event.</param>
    /// <returns>Number of domain events removed.</returns>
    public int AddOrReplace(IDomainEvent item)
    {
        var count = RemoveType(item.GetType());
        Add(item);
        return count;
    }

    /// <summary>
    /// Removes a domain event.
    /// </summary>
    /// <param name="item">Domain event.</param>
    /// <returns><see langword="true"/> if the domain event was removed, otherwise <see langword="false"/>.</returns>
    public bool Remove(IDomainEvent item)
    {
        return _domainEvents.Remove(item);
    }

    /// <summary>
    /// Removes all types of a domain event.
    /// </summary>
    /// <typeparam name="T">Domain event type.</typeparam>
    /// <returns>Number of domain events removed.</returns>
    public int RemoveType<T>() where T : IDomainEvent
    {
        return RemoveType(typeof(T));
    }

    /// <summary>
    /// Removes all types of a domain event.
    /// </summary>
    /// <param name="type">Domain event type.</param>
    /// <returns>Number of domain events removed.</returns>
    public int RemoveType(Type type)
    {
        var toRemove = _domainEvents.Where(e => e.GetType() == type).ToList();

        foreach (var item in toRemove)
        {
            _domainEvents.Remove(item);
        }

        return toRemove.Count;
    }

    /// <summary>
    /// Clears all domain events.
    /// </summary>
    public void Clear()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// Determines whether a domain event was added.
    /// </summary>
    /// <param name="item">Domain event.</param>
    /// <returns><see langword="true"/> if the domain event was added, otherwise <see langword="false"/>.</returns>
    public bool Contains(IDomainEvent item)
    {
        return _domainEvents.Contains(item);
    }

    /// <summary>
    /// Determines whether a domain event type was added.
    /// </summary>
    /// <typeparam name="T">Domain event type.</typeparam>
    /// <returns><see langword="true"/> if the domain event type was added, otherwise <see langword="false"/>.</returns>
    public bool ContainsType<T>() where T : IDomainEvent
    {
        return ContainsType(typeof(T));
    }

    /// <summary>
    /// Determines whether a domain event type was added.
    /// </summary>
    /// <param name="type">Domain event type.</param>
    /// <returns><see langword="true"/> if the domain event type was added, otherwise <see langword="false"/>.</returns>
    public bool ContainsType(Type type)
    {
        return _domainEvents.Any(e => e.GetType() == type);
    }

    /// <summary>
    /// Copies domain events to an array.
    /// </summary>
    /// <param name="array">Array to copy to.</param>
    /// <param name="arrayIndex">Array starting index.</param>
    public void CopyTo(IDomainEvent[] array, int arrayIndex)
    {
        _domainEvents.CopyTo(array, arrayIndex);
    }

    /// <summary>
    /// Domain event enumerator.
    /// </summary>
    /// <returns>Enumerator.</returns>
    public IEnumerator<IDomainEvent> GetEnumerator()
    {
        return _domainEvents.GetEnumerator();
    }

    /// <summary>
    /// Domain event enumerator.
    /// </summary>
    /// <returns>Enumerator.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return _domainEvents.GetEnumerator();
    }
}
