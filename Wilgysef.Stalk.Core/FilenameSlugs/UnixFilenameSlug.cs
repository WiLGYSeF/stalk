using System.Text;
using Wilgysef.Stalk.Core.Shared.Dependencies;
using Wilgysef.Stalk.Core.Shared.FilenameSlugs;

namespace Wilgysef.Stalk.Core.FilenameSlugs;

public class UnixFilenameSlug : IFilenameSlug, ITransientDependency
{
    public string Name => "Unix";

    public char PathSeparator => '/';

    // TODO: support turning off unicode
    public bool UseUnicode { get => _useUnicode; set => throw new NotImplementedException(); }
    private bool _useUnicode = true;

    public string SlugifyPath(string path)
    {
        if (path.Length == 0)
        {
            return "";
        }

        var parts = GetPathParts(path);
        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = SlugifyPart(parts[i]);
        }
        return string.Join(PathSeparator, parts);
    }

    public string SlugifyFile(string filename)
    {
        return SlugifyPart(filename);
    }

    public string Slugify(string path, string filename)
    {
        var slugPath = SlugifyPath(path);
        var slugFile = SlugifyFile(filename);
        return slugPath.EndsWith(PathSeparator)
            ? slugPath + slugFile
            : slugPath + PathSeparator + slugFile;
    }

    private static string SlugifyPart(string part)
    {
        if (part.Length == 0)
        {
            return "";
        }
        if (part == ".")
        {
            return "\u00b7";
        }
        if (part == "..")
        {
            return "\u00b7\u00b7";
        }

        var builder = new StringBuilder();
        for (var i = 0; i < part.Length; i++)
        {
            builder.Append(part[i] != '/' ? part[i] : '\u2215');
        }
        return builder.ToString();
    }

    private string[] GetPathParts(string path)
    {
        return path.Split(PathSeparator);
    }
}
