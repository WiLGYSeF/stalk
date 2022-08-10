namespace Wilgysef.Stalk.Core.Models;

public interface IEntity
{
    public DomainEventCollection DomainEvents { get; }
}
