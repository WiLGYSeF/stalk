namespace Wilgysef.Stalk.Core.FilenameSlugs;

public interface IFilenameSlugSelector
{
    /// <summary>
    /// Gets the matching filename slug by environment platform name.
    /// </summary>
    /// <returns>Filename slug.</returns>
    IFilenameSlug GetFilenameSlugByPlatform();
}
