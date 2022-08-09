using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Core.DomainEvents;

public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceLocator _serviceLocator;

    public DomainEventDispatcher(IServiceLocator serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    public async Task DispatchEvents<T>(params T[] eventData) where T : notnull, IDomainEvent
    {
        var eventHandlerType = typeof(IDomainEventHandler<>);

        foreach (var data in eventData)
        {
            var genericType = eventHandlerType.MakeGenericType(data.GetType());

            var service = _serviceLocator.GetRequiredService(genericType);

            dynamic handlerWrapper = Activator.CreateInstance(
                typeof(DomainEventHandlerWrapper<>).MakeGenericType(data.GetType()),
                service)!;

            await handlerWrapper.HandleEventAsync(data);
        }
    }
}
