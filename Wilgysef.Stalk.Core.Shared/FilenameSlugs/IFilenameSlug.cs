namespace Wilgysef.Stalk.Core.Shared.FilenameSlugs
{
    public interface IFilenameSlug
    {
        /// <summary>
        /// Filename slug name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Path separator.
        /// </summary>
        char PathSeparator { get; }

        /// <summary>
        /// Indicates if unicode characters are allowed.
        /// </summary>
        bool UseUnicode { get; set; }

        /// <summary>
        /// Slugifies the path.
        /// </summary>
        /// <param name="path">Path.</param>
        /// <returns>Slugified path.</returns>
        string SlugifyPath(string path);

        /// <summary>
        /// Slugifies the filename.
        /// </summary>
        /// <param name="file">Filename.</param>
        /// <returns>Slugified filename.</returns>
        string SlugifyFile(string file);

        /// <summary>
        /// Slugifies the directory and filename.
        /// </summary>
        /// <param name="path">Directory path.</param>
        /// <param name="filename">Filename.</param>
        /// <returns>Full slugified path of directory and file.</returns>
        string Slugify(string path, string filename);
    }
}
