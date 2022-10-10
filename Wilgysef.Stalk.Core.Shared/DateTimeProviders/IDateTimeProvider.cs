using System;

namespace Wilgysef.Stalk.Core.Shared.DateTimeProviders
{
    public interface IDateTimeProvider
    {
        public DateTime Now { get; }

        public DateTime Today { get; }

        public DateTime UtcNow { get; }

        public DateTimeOffset OffsetNow { get; }

        public DateTimeOffset OffsetUtcNow { get; }
    }
}
