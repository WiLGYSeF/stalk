namespace Wilgysef.Stalk.Core.DomainEvents.Events;

public class JobCreatedEvent : IDomainEvent
{
    public long Id { get; }

    public JobCreatedEvent(long id)
    {
        Id = id;
    }

    public override string ToString()
    {
        return "a";
    }
}
