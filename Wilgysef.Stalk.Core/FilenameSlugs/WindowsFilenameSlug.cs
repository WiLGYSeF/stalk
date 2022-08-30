using System.Text;

namespace Wilgysef.Stalk.Core.FilenameSlugs;

public class WindowsFilenameSlug : IFilenameSlug
{
    public string Name => "Windows";

    public char PathSeparator => '\\';

    public char PathSeparatorAlt => '/';

    public char VolumeSeparator => ':';

    private bool _useUnicode = true;

    // TODO: support turning off unicode
    public bool UseUnicode { get => _useUnicode; set => throw new NotImplementedException(); }

    private static HashSet<string> _specialFilenames = new()
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

        var driveLetter = GetDriveLetter(path);
        if (driveLetter != null)
        {
            path = path[2..];
        }

        var parts = GetPathParts(path);
        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = SlugifyPart(parts[i]);
        }

        var result = string.Join(PathSeparator, parts);
        if (driveLetter != null)
        {
            result = driveLetter.ToString() + VolumeSeparator + result;
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
