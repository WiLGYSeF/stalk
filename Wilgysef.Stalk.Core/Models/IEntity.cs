namespace Wilgysef.Stalk.Core.Models;

public interface IEntity
{
    /// <summary>
    /// Domain events, fired when entity changes are saved.
    /// </summary>
    public DomainEventCollection DomainEvents { get; }
}
