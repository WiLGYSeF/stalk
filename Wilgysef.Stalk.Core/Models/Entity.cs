using System.ComponentModel.DataAnnotations.Schema;
using Wilgysef.Stalk.Core.DomainEvents;

namespace Wilgysef.Stalk.Core.Models;

public abstract class Entity : IEntity
{
    [NotMapped]
    public DomainEventCollection DomainEvents { get; } = new();
}
