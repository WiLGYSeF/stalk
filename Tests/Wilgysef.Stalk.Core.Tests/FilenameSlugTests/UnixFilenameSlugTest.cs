using Shouldly;
using Wilgysef.Stalk.Core.FilenameSlugs;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Core.Tests.FilenameSlugTests;

public class UnixFilenameSlugTest : BaseTest
{
    private readonly IFilenameSlug _unixFilenameSlug;

    public UnixFilenameSlugTest()
    {
        _unixFilenameSlug = GetRequiredService<IEnumerable<IFilenameSlug>>()
            .Single(s => s.Name == "Unix");
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("/test/asdf", "/test/asdf")]
    [InlineData("abcdef", "abcdef")]
    [InlineData("/test/./abc", "/test/\u00b7/abc")]
    [InlineData("/test/../abc", "/test/\u00b7\u00b7/abc")]
    public void Slug_Path(string path, string expected)
    {
        _unixFilenameSlug.SlugifyPath(path).ShouldBe(expected);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("abcdef", "abcdef")]
    [InlineData(".", "\u00b7")]
    [InlineData("..", "\u00b7\u00b7")]
    [InlineData("...", "...")]
    [InlineData("test/abc", "test\u2215abc")]
    public void Slug_Filename(string filename, string expected)
    {
        _unixFilenameSlug.SlugifyFile(filename).ShouldBe(expected);
    }

    [Theory]
    [InlineData("/test/./asdf", "a/aa", "/test/\u00b7/asdf/a\u2215aa")]
    [InlineData("/test/./asdf/", "a/aa", "/test/\u00b7/asdf/a\u2215aa")]
    public void Slug_Path_Filename(string path, string filename, string expected)
    {
        _unixFilenameSlug.Slugify(path, filename).ShouldBe(expected);
    }
}
