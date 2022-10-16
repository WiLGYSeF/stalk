using Wilgysef.Stalk.Core.Downloaders;
using Wilgysef.Stalk.Core.Shared.Downloaders;

namespace Wilgysef.Stalk.Core.DownloadSelectors;

public interface IDownloadSelector
{
    /// <summary>
    /// Gets a downloader that can download from the URI.
    /// <see cref="DefaultDownloader"/> is given lower priority to any other downloaders.
    /// </summary>
    /// <param name="uri">URI.</param>
    /// <returns>Downloader, or <see langword="null"/> if none could be found.</returns>
    IDownloader? SelectDownloader(Uri uri);
}
