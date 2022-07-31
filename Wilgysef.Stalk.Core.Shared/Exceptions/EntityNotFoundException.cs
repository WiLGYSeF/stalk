using System;

namespace Wilgysef.Stalk.Core.Shared.Exceptions
{
    public class EntityNotFoundException : Exception
    {
        public string EntityName { get; private set; }

        public object EntityId { get; private set; }

        public EntityNotFoundException(string entityName, object entityId)
        {
            EntityName = entityName;
            EntityId = entityId;
        }
    }
}
