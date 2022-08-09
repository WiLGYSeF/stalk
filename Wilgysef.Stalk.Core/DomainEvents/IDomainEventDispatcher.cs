using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.DomainEvents;

public interface IDomainEventDispatcher : ITransientDependency
{
    Task DispatchEvents<T>(IEnumerable<T> eventData) where T : IDomainEvent;
}
