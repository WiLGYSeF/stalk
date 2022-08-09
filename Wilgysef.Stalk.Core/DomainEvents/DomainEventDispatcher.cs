using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Core.DomainEvents;

public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceLocator _serviceLocator;

    public DomainEventDispatcher(IServiceLocator serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    public async Task DispatchEvents<T>(IEnumerable<T> eventData) where T : IDomainEvent
    {
        var genericType = typeof(IDomainEventHandler<>).MakeGenericType(typeof(T));

        foreach (var data in eventData)
        {
            dynamic service = _serviceLocator.GetRequiredService(genericType);
            await service.HandleEventAsync(data);
        }
    }
}
