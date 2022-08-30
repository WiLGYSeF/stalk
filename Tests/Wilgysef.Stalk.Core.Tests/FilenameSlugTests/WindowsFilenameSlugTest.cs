using Shouldly;
using Wilgysef.Stalk.Core.FilenameSlugs;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Core.Tests.FilenameSlugTests;

public class WindowsFilenameSlugTest : BaseTest
{
    private readonly IFilenameSlug _windowsFilenameSlug;

    public WindowsFilenameSlugTest()
    {
        _windowsFilenameSlug = GetRequiredService<IEnumerable<IFilenameSlug>>()
            .Single(s => s.Name == "Windows");
    }

    [Theory]
    [InlineData("", "")]
    [InlineData(@"\test\asdf", @"\test\asdf")]
    [InlineData("/test/asdf", @"\test\asdf")]
    [InlineData("abcdef", "abcdef")]
    [InlineData(@"\test\.\abc", "\\test\\\u00b7\\abc")]
    [InlineData(@"\test\..\abc", "\\test\\\u00b7\u00b7\\abc")]
    [InlineData(@"C:\test\asdf", @"C:\test\asdf")]
    [InlineData(@"C:\test\asdf?", "C:\\test\\asdf\uff1f")]
    [InlineData(@"C:test\asdf", @"C:test\asdf")]
    [InlineData(@"C:test\asdf?", "C:test\\asdf\uff1f")]
    [InlineData("_:", "_\uff1a")]
    [InlineData(@"<>/:""\|?*", "\uff1c\uff1e\\\uff1a\u201c\\\uff5c\uff1f\uff0a")]
    [InlineData(@"\\.\C:\test\asdf", @"\\.\C:\test\asdf")]
    [InlineData(@"\\?\C:\test\asdf", @"\\?\C:\test\asdf")]
    [InlineData(@"\\server\share\test\asdf", @"\\server\share\test\asdf")]
    public void Slug_Path(string path, string expected)
    {
        _windowsFilenameSlug.SlugifyPath(path).ShouldBe(expected);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("abcdef", "abcdef")]
    [InlineData(".", "\u00b7")]
    [InlineData("..", "\u00b7\u00b7")]
    [InlineData("...", "...")]
    [InlineData(@"test\abc", "test\uff3cabc")]
    [InlineData("test/abc", "test\u2215abc")]
    [InlineData(@"C:\test\asdf", "C\uff1a\uff3ctest\uff3casdf")]
    [InlineData(@"<>/:""\|?*", "\uff1c\uff1e\u2215\uff1a\u201c\uff3c\uff5c\uff1f\uff0a")]
    public void Slug_Filename(string filename, string expected)
    {
        _windowsFilenameSlug.SlugifyFile(filename).ShouldBe(expected);
    }

    [Theory]
    [InlineData(@"C:\test?asdf", @"a\aa", "C:\\test\uff1fasdf\\a\uff3caa")]
    [InlineData(@"C:\test?asdf\", @"a\aa", "C:\\test\uff1fasdf\\a\uff3caa")]
    [InlineData("/test?asdf", @"a/aa", "\\test\uff1fasdf\\a\u2215aa")]
    [InlineData("/test?asdf/", @"a/aa", "\\test\uff1fasdf\\a\u2215aa")]
    public void Slug_Path_Filename(string path, string filename, string expected)
    {
        _windowsFilenameSlug.Slugify(path, filename).ShouldBe(expected);
    }
}
