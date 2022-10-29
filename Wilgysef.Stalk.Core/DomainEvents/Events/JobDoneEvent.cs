namespace Wilgysef.Stalk.Core.DomainEvents.Events;

public class JobDoneEvent : IDomainEvent
{
    public long JobId { get; }

    public JobDoneEvent(long jobId)
    {
        JobId = jobId;
    }
}
