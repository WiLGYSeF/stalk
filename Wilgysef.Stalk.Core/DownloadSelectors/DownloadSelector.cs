using Wilgysef.Stalk.Core.Downloaders;
using Wilgysef.Stalk.Core.Shared.Dependencies;
using Wilgysef.Stalk.Core.Shared.Downloaders;

namespace Wilgysef.Stalk.Core.DownloadSelectors;

public class DownloadSelector : IDownloadSelector, ITransientDependency
{
    private readonly IEnumerable<IDownloader> _downloaders;

    public DownloadSelector(
        IEnumerable<IDownloader> downloaders)
    {
        _downloaders = downloaders;
    }

    public IDownloader? SelectDownloader(Uri uri)
    {
        var defaultDownloader = _downloaders.FirstOrDefault(d => d is DefaultDownloader)
            ?? _downloaders.SingleOrDefault();
        return _downloaders.FirstOrDefault(d => d is not DefaultDownloader && d.CanDownload(uri))
            ?? defaultDownloader;
    }
}
