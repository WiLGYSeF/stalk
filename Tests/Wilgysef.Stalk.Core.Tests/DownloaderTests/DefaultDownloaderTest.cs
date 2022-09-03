using Moq;
using Moq.Protected;
using Shouldly;
using Wilgysef.Stalk.Core.Downloaders;
using Wilgysef.Stalk.Core.MetadataObjects;
using Wilgysef.Stalk.Core.Shared.Downloaders;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Core.Tests.DownloaderTests;

public class DefaultDownloaderTest : BaseTest
{
    private readonly IDefaultDownloader _downloader;

    public DefaultDownloaderTest()
    {
        RegisterDownloaders = true;

        ReplaceServiceDelegate(c => new HttpClient(new MockMessageHandler()));

        var downloaders = GetRequiredService<IEnumerable<IDownloader>>();
        _downloader = downloaders.Single(d => d is IDefaultDownloader) as IDefaultDownloader;
    }

    class MockMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    [Fact]
    public async Task Test()
    {
        await foreach (var result in _downloader.DownloadAsync(
            new Uri(RandomValues.RandomUri()),
            RandomValues.RandomString(10),
            RandomValues.RandomString(10),
            RandomValues.RandomString(10),
            RandomValues.RandomString(10),
            new MetadataObject('.')))
        {

        }
    }

    [Fact]
    public void Can_Always_Download()
    {
        _downloader.CanDownload(null).ShouldBeTrue();
    }
}
