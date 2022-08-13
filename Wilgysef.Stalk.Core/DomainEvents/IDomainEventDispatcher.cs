using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.DomainEvents;

public interface IDomainEventDispatcher : ITransientDependency
{
    Task DispatchEvents<T>(params T[] eventData) where T : notnull, IDomainEvent;
}
