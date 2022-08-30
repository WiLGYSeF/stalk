using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.FilenameSlugs;

public interface IFilenameSlug : ITransientDependency
{
    string Name { get; }

    char PathSeparator { get; }

    bool UseUnicode { get; set; }

    string SlugifyPath(string path);

    string SlugifyFile(string file);

    string Slugify(string path, string filename);
}
