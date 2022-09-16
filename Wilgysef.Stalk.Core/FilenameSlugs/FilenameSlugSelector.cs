using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.FilenameSlugs;

public class FilenameSlugSelector : IFilenameSlugSelector, ITransientDependency
{
    private readonly IEnumerable<IFilenameSlug> _filenameSlugs;

    public FilenameSlugSelector(IEnumerable<IFilenameSlug> filenameSlugs)
    {
        _filenameSlugs = filenameSlugs;
    }

    public IFilenameSlug GetFilenameSlugByPlatform()
    {
        return Environment.OSVersion.Platform switch
        {
            PlatformID.Win32NT => _filenameSlugs.Single(s => s is WindowsFilenameSlug),
            PlatformID.Unix => _filenameSlugs.Single(s => s is UnixFilenameSlug),
            _ => throw new NotImplementedException("OSVersion platform not supported."),
        };
    }
}
