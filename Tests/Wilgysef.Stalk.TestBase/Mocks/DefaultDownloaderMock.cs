using Wilgysef.Stalk.Core.Downloaders;
using Wilgysef.Stalk.Core.Shared.Downloaders;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;

namespace Wilgysef.Stalk.TestBase.Mocks;

public class DefaultDownloaderMock : IDefaultDownloader
{
    public bool CanDownload(Uri uri)
    {
        return true;
    }

    public async IAsyncEnumerable<DownloadResult> DownloadAsync(Uri uri, string itemData, IMetadataObject metadata, CancellationToken cancellationToken = default)
    {
        yield break;
    }
}
