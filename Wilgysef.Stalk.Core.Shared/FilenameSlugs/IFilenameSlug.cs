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
        /// Slugifies the directory path.
        /// </summary>
        /// <param name="path">Directory path.</param>
        /// <returns>Slugified directory path.</returns>
        string SlugifyPath(string path);

        /// <summary>
        /// Slugifies the file path.
        /// </summary>
        /// <param name="file">File path.</param>
        /// <returns>Slugified file path.</returns>
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
