using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.FilenameSlugs;

public interface IFilenameSlugSelector : ITransientDependency
{
    /// <summary>
    /// Gets the matching filename slug by environment platform name.
    /// </summary>
    /// <returns>Filename slug.</returns>
    IFilenameSlug GetFilenameSlugByPlatform();
}
