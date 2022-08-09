using Wilgysef.Stalk.Core.DomainEvents;

namespace Wilgysef.Stalk.Core.Models;

public abstract class Entity : IEntity
{
    public ICollection<IDomainEvent> DomainEvents => _domainEvents;

    private List<IDomainEvent> _domainEvents = new List<IDomainEvent>();
}
