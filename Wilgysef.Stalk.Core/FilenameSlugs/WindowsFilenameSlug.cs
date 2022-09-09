using System.Text;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.FilenameSlugs;

public class WindowsFilenameSlug : IFilenameSlug, ITransientDependency
{
    public string Name => "Windows";

    public char PathSeparator => '\\';

    public static char PathSeparatorAlt => '/';

    public static char VolumeSeparator => ':';

    private bool _useUnicode = true;

    // TODO: support turning off unicode
    public bool UseUnicode { get => _useUnicode; set => throw new NotImplementedException(); }

    private readonly static HashSet<string> _specialFilenames = new()
    {
        "AUX",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "CON",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
        "NUL",
        "PRN",
    };

    public string SlugifyPath(string path)
    {
        if (path.Length == 0)
        {
            return "";
        }

        var prefix = GetPathPrefix(path);
        if (prefix.Length > 0)
        {
            path = path[prefix.Length..];
        }

        var parts = GetPathParts(path);
        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = SlugifyPart(parts[i]);
        }

        var result = string.Join(PathSeparator, parts);
        if (prefix.Length > 0)
        {
            result = prefix + result;
        }
        return result;
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

    private string SlugifyPart(string part)
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

        if (_specialFilenames.Contains(part))
        {
            return part + "_";
        }

        var builder = new StringBuilder();
        for (var i = 0; i < part.Length; i++)
        {
            builder.Append(part[i] switch
            {
                '<' => '\uff1c',
                '>' => '\uff1e',
                ':' => '\uff1a',
                '"' => '\u201c',
                '/' => '\u2215',
                '\\' => '\uff3c',
                '|' => '\uff5c',
                '?' => '\uff1f',
                '*' => '\uff0a',
                _ => part[i],
            });
        }
        return builder.ToString();
    }

    private string GetPathPrefix(string path)
    {
        var driveLetter = GetDriveLetter(path);
        if (driveLetter != null)
        {
            return driveLetter.ToString() + VolumeSeparator;
        }

        var uncPathPrefix = GetUncPathPrefix(path);
        if (uncPathPrefix.HasValue)
        {
            return $@"\\{uncPathPrefix.Value.Server}\{uncPathPrefix.Value.Share}";
        }

        return "";
    }

    private (string Server, string Share)? GetUncPathPrefix(string path)
    {
        if (!(path[0] == '\\' && path[1] == '\\'))
        {
            return null;
        }

        var firstIndex = path.IndexOf('\\', 2);
        if (firstIndex == path.Length - 1)
        {
            return null;
        }

        var secondIndex = path.IndexOf('\\', firstIndex + 1);
        return (path[2..firstIndex], path[(firstIndex + 1)..secondIndex]);
    }

    private char? GetDriveLetter(string path)
    {
        return path.Length >= 2 && path[1] == VolumeSeparator && char.IsLetter(path[0])
            ? path[0]
            : null;
    }

    private string[] GetPathParts(string path)
    {
        return path.Split(PathSeparator, PathSeparatorAlt);
    }
}
