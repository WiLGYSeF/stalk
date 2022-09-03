namespace Wilgysef.Stalk.Core.FilenameSlugs;

internal class FilenameSlugSelector : IFilenameSlugSelector
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
            _ => throw new NotImplementedException("OSVersion platform not supported"),
        };
    }
}
