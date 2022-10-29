using System.ComponentModel.DataAnnotations.Schema;

namespace Wilgysef.Stalk.Core.Models;

public abstract class Entity : IEntity
{
    [NotMapped]
    public DomainEventCollection DomainEvents { get; } = new();
}
