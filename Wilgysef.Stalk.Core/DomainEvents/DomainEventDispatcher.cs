using Wilgysef.Stalk.Core.Shared.Dependencies;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Core.DomainEvents;

public class DomainEventDispatcher : IDomainEventDispatcher, ITransientDependency
{
    private readonly IServiceLocator _serviceLocator;

    public DomainEventDispatcher(IServiceLocator serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    public async Task DispatchEventsAsync<T>(params T[] eventData) where T : notnull, IDomainEvent
    {
        await DispatchEventsAsync(eventData, default);
    }

    public async Task DispatchEventsAsync<T>(IEnumerable<T> eventData, CancellationToken cancellationToken = default) where T : notnull, IDomainEvent
    {
        var eventHandlerType = typeof(IDomainEventHandler<>);

        foreach (var data in eventData)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var dataType = data.GetType();
            var genericType = eventHandlerType.MakeGenericType(dataType);

            var genericEnumerableType = typeof(IEnumerable<>).MakeGenericType(genericType);
            var services = _serviceLocator.GetRequiredService(genericEnumerableType);

            var handlerWrapper = (IDomainEventHandlerWrapper)Activator.CreateInstance(
                typeof(DomainEventHandlerWrapper<>).MakeGenericType(dataType))!;

            await handlerWrapper.HandleEventAsync(services, data, cancellationToken);
        }
    }
}
