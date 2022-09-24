using Shouldly;
using System.Text;
using Wilgysef.Stalk.Core.Downloaders;
using Wilgysef.Stalk.Core.MetadataObjects;
using Wilgysef.Stalk.Core.Shared.Downloaders;
using Wilgysef.Stalk.TestBase;
using Wilgysef.Stalk.TestBase.Mocks;

namespace Wilgysef.Stalk.Core.Tests.DownloaderTests;

public class DataDownloaderTest : BaseTest
{
    private readonly MockFileService _fileService;
    private readonly DataDownloader _downloader;

    public DataDownloaderTest()
    {
        RegisterDownloaders = true;

        var downloaders = GetRequiredService<IEnumerable<IDownloader>>();
        _downloader = (downloaders.Single(d => d is DataDownloader) as DataDownloader)!;

        _fileService = MockFileService!;
    }

    [Fact]
    public async Task Download_File_And_Save_Metadata()
    {
        var data = Encoding.UTF8.GetBytes("this is a test");
        var uri = new Uri($"data:text/plain;base64,{Convert.ToBase64String(data)}");
        var filename = "testfile";
        var itemId = RandomValues.RandomString(10);
        var itemData = RandomValues.RandomString(10);
        var metadataFilename = "testmeta";
        var metadata = new MetadataObject('.');

        await foreach (var result in _downloader.DownloadAsync(
            uri,
            filename,
            itemId,
            itemData,
            metadataFilename,
            metadata))
        {
            result.Path.ShouldBe(filename);
            result.Uri.ShouldBe(uri);
            result.ItemId.ShouldBe(itemId);
            result.ItemData.ShouldBe(itemData);
            result.MetadataPath.ShouldBe(metadataFilename);
        }

        _fileService.Files.Keys.Count().ShouldBe(2);
        (_fileService.Files[filename] as MemoryStream)!.ToArray().ShouldBe(data);
    }

    [Theory]
    [InlineData("data:text/plain;base64,aGVsbG8gdGhlcmU=", true)]
    [InlineData("https://example.com", false)]
    public void Can_Download(string uri, bool downloadable)
    {
        _downloader.CanDownload(new Uri(uri)).ShouldBe(downloadable);
    }
}
