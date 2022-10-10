using Wilgysef.Stalk.Core.Shared.DateTimeProviders;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.DateTimeProviders;

public class DateTimeProvider : IDateTimeProvider, ITransientDependency
{
    public DateTime Now => DateTime.Now;

    public DateTime Today => DateTime.Today;

    public DateTime UtcNow => DateTime.UtcNow;

    public DateTimeOffset OffsetNow => DateTimeOffset.Now;

    public DateTimeOffset OffsetUtcNow => DateTimeOffset.UtcNow;
}
