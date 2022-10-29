using System;

namespace Wilgysef.Stalk.Core.Shared.Exceptions
{
    /// <summary>
    /// Entity was not found.
    /// </summary>
    public class EntityNotFoundException : Exception
    {
        /// <summary>
        /// Name of entity type.
        /// </summary>
        public string EntityName { get; private set; }

        /// <summary>
        /// Entity Id.
        /// </summary>
        public object EntityId { get; private set; }

        /// <summary>
        /// Entity was not found.
        /// </summary>
        public EntityNotFoundException(string entityName, object entityId)
        {
            EntityName = entityName;
            EntityId = entityId;
        }
    }
}
