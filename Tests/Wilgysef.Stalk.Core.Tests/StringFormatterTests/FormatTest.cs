using Shouldly;
using Wilgysef.Stalk.Core.StringFormatters;

namespace Wilgysef.Stalk.Core.Tests.StringFormatterTests;

public class FormatTest
{
    [Theory]
    [InlineData("", "")]
    [InlineData("abc test", "abc test")]
    [InlineData("{", "{")]
    [InlineData("}", "}")]
    [InlineData("{}", "")]
    [InlineData("{{}}", "{}}")]
    [InlineData("{{}", "{}")]
    [InlineData("{}}", "}")]
    [InlineData("{{{}", "{")]
    [InlineData("{{{{}", "{{}")]
    [InlineData("{{{{{}", "{{")]
    [InlineData("d{{asdf}b", "d{asdf}b")]
    public void Format_Strings(string format, string expected)
    {
        new StringFormatter().Format(format, new Dictionary<string, object>()).ShouldBe(expected);
    }

    [Theory]
    [InlineData("-{test}-", "-abc-")]
    [InlineData("-{notexist}-", "--")]
    [InlineData("-{notexist|test}-", "-abc-")]
    [InlineData("-{notexist|notexist}-", "--")]
    [InlineData("-{notexist|\"test}-", "-test-")]
    [InlineData("-{notexist|\"test\"}-", "-test-")]
    [InlineData("-{test}-{notexist}-{num}-", "-abc--123-")]
    [InlineData("-{test,5}-", "-  abc-")]
    [InlineData("-{test,-5}-", "-abc  -")]
    [InlineData("-{test,2}-", "-abc-")]
    [InlineData("-{test,-2}-", "-abc-")]
    public void Format_Strings_Replace(string format, string expected)
    {
        var replace = new Dictionary<string, object>
        {
            { "test", "abc" },
            { "num", 123 },
        };

        new StringFormatter().Format(format, replace).ShouldBe(expected);
    }
}
