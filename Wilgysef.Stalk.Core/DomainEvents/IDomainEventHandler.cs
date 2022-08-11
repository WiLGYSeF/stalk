using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.DomainEvents;

public interface IDomainEventHandler<T> : ITransientDependency where T : IDomainEvent
{
    Task HandleEventAsync(T eventData);
}
