namespace Wilgysef.Stalk.Core.DomainEvents.Events;

public class JobPriorityChangedEvent : IDomainEvent
{
    public long JobId { get; }

    public int OldJobPriority { get; }

    public int NewJobPriority { get; }

    public JobPriorityChangedEvent(long jobId, int oldJobPriority, int newJobPriority)
    {
        JobId = jobId;
        OldJobPriority = oldJobPriority;
        NewJobPriority = newJobPriority;
    }
}
