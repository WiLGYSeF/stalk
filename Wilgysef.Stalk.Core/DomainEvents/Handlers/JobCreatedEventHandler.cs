using Wilgysef.Stalk.Core.DomainEvents.Events;

namespace Wilgysef.Stalk.Core.DomainEvents.Handlers;

public class JobCreatedEventHandler : IDomainEventHandler<JobCreatedEvent>
{
    public Task HandleEventAsync(JobCreatedEvent eventData)
    {
        throw new NotImplementedException();
        return Task.CompletedTask;
    }
}
