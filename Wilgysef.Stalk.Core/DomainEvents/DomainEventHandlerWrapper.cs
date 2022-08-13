namespace Wilgysef.Stalk.Core.DomainEvents;

internal interface IDomainEventHandlerWrapper
{
    Task HandleEventAsync(object handlers, object eventData);
}

internal class DomainEventHandlerWrapper<T> : IDomainEventHandlerWrapper where T : IDomainEvent
{
    public async Task HandleEventAsync(object handlers, object eventData)
    {
        foreach (var handler in (IEnumerable<IDomainEventHandler<T>>)handlers)
        {
            await handler.HandleEventAsync((T)eventData);
        }
    }
}
