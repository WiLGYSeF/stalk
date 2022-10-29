namespace Wilgysef.Stalk.Core.DomainEvents;

internal interface IDomainEventHandlerWrapper
{
    Task HandleEventAsync(object handlers, object eventData, CancellationToken cancellationToken);
}

internal class DomainEventHandlerWrapper<T> : IDomainEventHandlerWrapper where T : IDomainEvent
{
    public async Task HandleEventAsync(object handlers, object eventData, CancellationToken cancellationToken)
    {
        foreach (var handler in (IEnumerable<IDomainEventHandler<T>>)handlers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await handler.HandleEventAsync((T)eventData, cancellationToken);
        }
    }
}
