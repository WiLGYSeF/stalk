namespace Wilgysef.Stalk.Core.DomainEvents;

public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatch events.
    /// </summary>
    /// <typeparam name="T">Event type.</typeparam>
    /// <param name="eventData">Event data.</param>
    /// <returns></returns>
    Task DispatchEventsAsync<T>(params T[] eventData) where T : notnull, IDomainEvent;

    /// <summary>
    /// Dispatch events.
    /// </summary>
    /// <typeparam name="T">Event type.</typeparam>
    /// <param name="eventData">Event data.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task DispatchEventsAsync<T>(IEnumerable<T> eventData, CancellationToken cancellationToken = default) where T : notnull, IDomainEvent;
}
