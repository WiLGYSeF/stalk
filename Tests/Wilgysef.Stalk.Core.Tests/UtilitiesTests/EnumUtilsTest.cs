using Shouldly;
using System.Runtime.Serialization;
using Wilgysef.Stalk.Core.Utilities;

namespace Wilgysef.Stalk.Core.Tests.UtilitiesTests;

public class EnumUtilsTest
{
    [Theory]
    [InlineData("Monday", false, Weekday.Monday)]
    [InlineData("MONDAY", true, Weekday.Monday)]
    [InlineData("MONDAY", false, null)]
    [InlineData("Friday", false, Weekday.Friday)]
    [InlineData("test", false, null)]
    [InlineData("1", false, Weekday.Monday)]
    [InlineData("10", false, null)]
    public void Parse_Enum(string value, bool ignoreCase, Weekday? expected)
    {
        var success = EnumUtils.TryParse<Weekday>(value, out var result, ignoreCase);

        if (expected.HasValue)
        {
            success.ShouldBeTrue();
            result.ShouldBe(expected.Value);

            EnumUtils.Parse<Weekday>(value, ignoreCase).ShouldBe(expected.Value);
        }
        else
        {
            success.ShouldBeFalse();

            Should.Throw<ArgumentException>(() => EnumUtils.Parse<Weekday>(value, ignoreCase));
        }
    }

    [Theory]
    [InlineData("Mon", false, true, Weekday.Monday)]
    [InlineData("MON", true, true, Weekday.Monday)]
    [InlineData("MON", false, true, null)]
    [InlineData("Friday", false, false, Weekday.Friday)]
    [InlineData("Friday", false, true, null)]
    [InlineData("test", false, false, null)]
    [InlineData("test", false, true, null)]
    [InlineData("1", false, false, Weekday.Monday)]
    [InlineData("1", false, true, null)]
    [InlineData("10", false, false, null)]
    public void Parse_EnumMemberValue(string value, bool ignoreCase, bool enumMemberValueRequired, Weekday? expected)
    {
        var success = EnumUtils.TryParseEnumMemberValue<Weekday>(
            value,
            out var result,
            ignoreCase: ignoreCase,
            enumMemberValueRequired: enumMemberValueRequired);

        if (expected.HasValue)
        {
            success.ShouldBeTrue();
            result.ShouldBe(expected.Value);

            Parse().ShouldBe(expected.Value);
        }
        else
        {
            success.ShouldBeFalse();

            Should.Throw<ArgumentException>(() => Parse());
        }

        Weekday Parse()
        {
            return EnumUtils.ParseEnumMemberValue<Weekday>(
                value,
                ignoreCase: ignoreCase,
                enumMemberValueRequired: enumMemberValueRequired);
        }
    }

    public enum Weekday
    {
        [EnumMember(Value = "Sun")]
        Sunday = 0,
        [EnumMember(Value = "Mon")]
        Monday = 1,
        [EnumMember(Value = "Tue")]
        Tuesday = 2,
        Wednesday = 3,
        Thursday = 4,
        Friday = 5,
        Saturday = 6,
    }
}
