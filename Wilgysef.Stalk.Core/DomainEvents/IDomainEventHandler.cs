namespace Wilgysef.Stalk.Core.DomainEvents;

public interface IDomainEventHandler<T> where T : IDomainEvent
{
    /// <summary>
    /// Handles event.
    /// </summary>
    /// <param name="eventData">Event data</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task HandleEventAsync(T eventData, CancellationToken cancellationToken = default);
}
