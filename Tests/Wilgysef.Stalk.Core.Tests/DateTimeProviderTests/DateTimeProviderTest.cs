using Shouldly;
using Wilgysef.Stalk.Core.DateTimeProviders;

namespace Wilgysef.Stalk.Core.Tests.DateTimeProviderTests;

public class DateTimeProviderTest
{
    [Fact]
    public void Get_DateTimes()
    {
        var dateTimeProvider = new DateTimeProvider();

        var epsilon = 500;
        (dateTimeProvider.Now - DateTime.Now).TotalMilliseconds.ShouldBeLessThanOrEqualTo(epsilon);
        (dateTimeProvider.Today - DateTime.Today).TotalMilliseconds.ShouldBeLessThanOrEqualTo(epsilon);
        (dateTimeProvider.UtcNow - DateTime.UtcNow).TotalMilliseconds.ShouldBeLessThanOrEqualTo(epsilon);
        (dateTimeProvider.OffsetNow - DateTimeOffset.Now).TotalMilliseconds.ShouldBeLessThanOrEqualTo(epsilon);
        (dateTimeProvider.OffsetUtcNow - DateTimeOffset.UtcNow).TotalMilliseconds.ShouldBeLessThanOrEqualTo(epsilon);
    }
}
