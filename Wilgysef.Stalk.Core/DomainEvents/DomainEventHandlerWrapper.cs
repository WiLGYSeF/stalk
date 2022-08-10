namespace Wilgysef.Stalk.Core.DomainEvents;

internal interface IDomainEventHandlerWrapper
{
    Task HandleEventAsync(object handler, object eventData);
}

internal class DomainEventHandlerWrapper<T> : IDomainEventHandlerWrapper where T : IDomainEvent
{
    public async Task HandleEventAsync(object handler, object eventData)
    {
        await ((IDomainEventHandler<T>)handler).HandleEventAsync((T)eventData);
    }
}
