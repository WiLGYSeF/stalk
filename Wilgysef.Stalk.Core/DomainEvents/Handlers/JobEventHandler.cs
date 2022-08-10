using Wilgysef.Stalk.Core.DomainEvents.Events;

namespace Wilgysef.Stalk.Core.DomainEvents.Handlers;

public class JobEventHandler :
    IDomainEventHandler<JobCreatedEvent>,
    IDomainEventHandler<JobStateChangedEvent>,
    IDomainEventHandler<JobPriorityChangedEvent>
{
    public async Task HandleEventAsync(JobCreatedEvent eventData)
    {
        await WorkPrioritizedJobs();
    }

    public async Task HandleEventAsync(JobStateChangedEvent eventData)
    {
        await WorkPrioritizedJobs();
    }

    public async Task HandleEventAsync(JobPriorityChangedEvent eventData)
    {
        await WorkPrioritizedJobs();
    }

    private async Task WorkPrioritizedJobs()
    {
        // TODO: start background job
    }
}
