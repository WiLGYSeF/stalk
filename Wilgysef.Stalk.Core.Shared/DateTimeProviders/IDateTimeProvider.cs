using System;

namespace Wilgysef.Stalk.Core.Shared.DateTimeProviders
{
    public interface IDateTimeProvider
    {
        /// <summary>
        /// Current local date and time.
        /// </summary>
        public DateTime Now { get; }

        /// <summary>
        /// Today's date, with time component set to 00:00:00.
        /// </summary>
        public DateTime Today { get; }

        /// <summary>
        /// Current UTC date and time.
        /// </summary>
        public DateTime UtcNow { get; }

        /// <summary>
        /// Current date and time, with local time offset.
        /// </summary>
        public DateTimeOffset OffsetNow { get; }

        /// <summary>
        /// Current date and time, with UTC time offset.
        /// </summary>
        public DateTimeOffset OffsetUtcNow { get; }
    }
}
