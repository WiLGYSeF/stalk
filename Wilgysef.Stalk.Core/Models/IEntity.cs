using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wilgysef.Stalk.Core.DomainEvents;

namespace Wilgysef.Stalk.Core.Models;

public interface IEntity
{
    ICollection<IDomainEvent> DomainEvents { get; }
}
