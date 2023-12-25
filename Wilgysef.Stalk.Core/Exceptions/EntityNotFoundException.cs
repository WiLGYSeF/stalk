using System;
using Wilgysef.Core.Exceptions;

namespace Wilgysef.Stalk.Core.Exceptions
{
    /// <summary>
    /// Entity was not found.
    /// </summary>
    public class EntityNotFoundException : NotFoundException
    {
        /// <summary>
        /// Name of entity type.
        /// </summary>
        public string EntityName { get; private set; }

        /// <summary>
        /// Entity Id.
        /// </summary>
        public object EntityId { get; private set; }

        public override string Code => "EntityNotFound";

        /// <summary>
        /// Entity was not found.
        /// </summary>
        public EntityNotFoundException(string entityName, object entityId)
            : base($"Could not find entity {entityName}", null)
        {
            EntityName = entityName;
            EntityId = entityId;
        }
    }
}
