namespace Wilgysef.Stalk.Core.DomainEvents;

internal class DomainEventHandlerWrapper<T> where T : IDomainEvent
{
    private readonly IDomainEventHandler<T> _handler;

    public DomainEventHandlerWrapper(IDomainEventHandler<T> handler)
    {
        _handler = handler;
    }

    public async Task HandleEventAsync(object eventData)
    {
        await _handler.HandleEventAsync((T)eventData);
    }
}
