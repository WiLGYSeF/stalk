using Shouldly;
using Wilgysef.Stalk.Core.StringFormatters;

namespace Wilgysef.Stalk.Core.Tests.StringFormatterTests;

public class FormatTest
{
    [Theory]
    [InlineData("", "")]
    [InlineData("abc test", "abc test")]
    [InlineData("$${test}", "${test}")]
    [InlineData("abc$test", "abc$test")]
    [InlineData("abc$$test", "abc$test")]
    [InlineData("abc$$$test", "abc$$test")]
    [InlineData("abc$$$$test", "abc$$test")]
    [InlineData("${}", "${}")]
    [InlineData("${{test}", "")]
    [InlineData("${{test}}", "}")]
    [InlineData("${a}", "")]
    [InlineData("${test}", "abc")]
    [InlineData("${test}}", "abc}")]
    [InlineData("${test", "${test")]
    [InlineData("${test}${test}", "abcabc")]
    [InlineData("${test}${num}", "abc123")]
    [InlineData("a${test}a${test}a", "aabcaabca")]
    [InlineData("${,4}", "    ")]
    [InlineData("${num,4}", " 123")]
    [InlineData("${num,-4}", "123 ")]
    [InlineData("${num,2}", "123")]
    [InlineData("${num,-2}", "123")]
    [InlineData("${num,aaa}", "123")]
    [InlineData("${|}", "")]
    [InlineData("${test|num}", "abc")]
    [InlineData("${notexist|}", "")]
    [InlineData("${notexist|test}", "abc")]
    [InlineData("${notexist|aaa}", "")]
    [InlineData("${notexist|aaa|num}", "123")]
    [InlineData("${notexist|aaa|\"asdf\"}", "asdf")]
    [InlineData("${notexist|\"test\"}", "test")]
    [InlineData("${notexist|\"test\"a}", "test")]
    [InlineData("${notexist|\"test\\\"}", "test\\")]
    [InlineData("${notexist|\"test}", "")]
    [InlineData("${notexist|\"te\\\"st\"}", "te\"st")]
    [InlineData("${notexist|\"te\\\\\"st\"}", "te\\\"st")]
    [InlineData("${notexist|\"te,st\"}", "te,st")]
    [InlineData("${notexist|\"test\"|test}", "test")]
    [InlineData("${test,4|num}", " abc")]
    [InlineData("${notexist,4|num}", " 123")]
    public void Format_Strings(string format, string expected)
    {
        var replace = new Dictionary<string, object>
        {
            { "test", "abc" },
            { "num", 123 },
        };

        new StringFormatter().Format(format, replace).ShouldBe(expected);
    }
}
