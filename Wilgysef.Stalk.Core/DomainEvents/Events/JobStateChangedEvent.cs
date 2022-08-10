using Wilgysef.Stalk.Core.Shared.Enums;

namespace Wilgysef.Stalk.Core.DomainEvents.Events;

public class JobStateChangedEvent : IDomainEvent
{
    public long JobId { get; }

    public JobState OldState { get; }

    public JobState NewState { get; }

    public JobStateChangedEvent(long jobId, JobState oldState, JobState newState)
    {
        JobId = jobId;
        OldState = oldState;
        NewState = newState;
    }
}
