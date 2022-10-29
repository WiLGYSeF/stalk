using System;
using Wilgysef.Stalk.Core.Shared.DateTimeProviders;

namespace Wilgysef.Stalk.TestBase.Shared.Mocks
{
    public class MockDateTimeProvider : IDateTimeProvider
    {
        public DateTime Now { get; set; } = DateTime.Now;

        public DateTime Today { get; set; } = DateTime.Today;

        public DateTime UtcNow { get; set; } = DateTime.UtcNow;

        public DateTimeOffset OffsetNow { get; set; } = DateTimeOffset.Now;

        public DateTimeOffset OffsetUtcNow { get; set; } = DateTimeOffset.UtcNow;

        public MockDateTimeProvider() { }

        public MockDateTimeProvider(DateTime dateTime)
        {
            SetDateTime(dateTime);
        }

        public MockDateTimeProvider(DateTimeOffset dateTimeOffset)
        {
            SetDateTimeOffset(dateTimeOffset);
        }

        public MockDateTimeProvider(DateTime dateTime, DateTimeOffset dateTimeOffset)
        {
            SetDateTime(dateTime);
            SetDateTimeOffset(dateTimeOffset);
        }

        public void SetDateTime(DateTime dateTime)
        {
            Now = dateTime;
            Today = dateTime.Date;
            UtcNow = dateTime.ToUniversalTime();
        }

        public void SetDateTimeOffset(DateTimeOffset dateTimeOffset)
        {
            OffsetNow = dateTimeOffset;
            OffsetUtcNow = dateTimeOffset.ToUniversalTime();
        }
    }
}
