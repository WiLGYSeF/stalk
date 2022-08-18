using Wilgysef.Stalk.Core.Shared.Downloaders;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;

namespace Wilgysef.Stalk.Core.Downloaders;

internal class DefaultDownloader : IDefaultDownloader
{
    public bool CanDownload(Uri uri)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<DownloadResult> DownloadAsync(Uri uri, string itemData, IMetadataObject metadata)
    {
        throw new NotImplementedException();
    }
}
