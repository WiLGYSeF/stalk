namespace Wilgysef.Stalk.Core.DomainEvents.Events;

public class JobCreatedEvent : IDomainEvent
{
    public long JobId { get; }

    public JobCreatedEvent(long jobId)
    {
        JobId = jobId;
    }
}
