using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.DomainEvents;

public interface IDomainEventDispatcher : ITransientDependency
{
    /// <summary>
    /// Dispatch events.
    /// </summary>
    /// <typeparam name="T">Event type.</typeparam>
    /// <param name="eventData">Event data.</param>
    /// <returns></returns>
    Task DispatchEvents<T>(params T[] eventData) where T : notnull, IDomainEvent;
}
