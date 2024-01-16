using Shouldly;
using Wilgysef.Stalk.Application.Services;

namespace Wilgysef.Stalk.Application.Tests.Services;

public class OutputPathServiceTest
{
    [Theory]
    [InlineData("test", "abc/test")]
    [InlineData("test/asdf", "abc/test/asdf")]
    [InlineData("test/asdf/", "abc/test/asdf/")]
    [InlineData("/../a", "abc/a")]
    [InlineData("../a", "abc/a")]
    [InlineData("a/../b", "abc/a/b")]
    [InlineData("a/..", "abc/a/")]
    [InlineData("a/./b", "abc/a/b")]
    [InlineData("a/.", "abc/a/")]
    public void GetOutputPath(string path, string expected)
    {
        var service = new OutputPathService
        {
            PathPrefix = "abc",
        };

        service.GetOutputPath(path)
            .Replace('\\', '/')
            .ShouldBe(expected);
    }
}
