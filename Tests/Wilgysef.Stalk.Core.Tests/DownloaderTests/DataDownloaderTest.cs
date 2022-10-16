using Shouldly;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using Wilgysef.Stalk.Core.Downloaders;
using Wilgysef.Stalk.Core.MetadataObjects;
using Wilgysef.Stalk.Core.Shared.Downloaders;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Extractors;
using Wilgysef.Stalk.TestBase;
using Wilgysef.Stalk.TestBase.Shared.Mocks;

namespace Wilgysef.Stalk.Core.Tests.DownloaderTests;

public class DataDownloaderTest : BaseTest
{
    private readonly DataDownloader _downloader;

    private readonly MockFileSystem _fileSystem;

    public DataDownloaderTest()
    {
        RegisterDownloaders = true;

        var downloaders = GetRequiredService<IEnumerable<IDownloader>>();
        _downloader = (downloaders.Single(d => d is DataDownloader) as DataDownloader)!;

        _fileSystem = MockFileSystem!;
    }

    [Theory]
    [InlineData("data:text/plain;base64,aGVsbG8gdGhlcmU=", true)]
    [InlineData("data:;base64,aGVsbG8gdGhlcmU=", true)]
    [InlineData("data:,test", true)]
    [InlineData("https://example.com", false)]
    public void Can_Download(string uri, bool expected)
    {
        _downloader.CanDownload(new Uri(uri)).ShouldBe(expected);
    }

    [Fact]
    public async Task Download_Data_Mediatype_Base64()
    {
        var data = Encoding.UTF8.GetBytes("this is a test");
        var uri = new Uri($"data:text/plain;base64,{Convert.ToBase64String(data)}");
        var filename = "testfile";
        var itemId = RandomValues.RandomString(10);
        var metadataFilename = "testmeta";
        var metadata = new MetadataObject('.');

        await foreach (var result in _downloader.DownloadAsync(
            uri,
            filename,
            itemId,
            metadataFilename,
            metadata))
        {
            result.Path.ShouldBe(filename);
            result.Uri.ShouldBe(uri);
            result.ItemId.ShouldBe(itemId);
            result.MetadataPath.ShouldBe(metadataFilename);
        }

        _fileSystem.AllFiles.Count().ShouldBe(2);
        _fileSystem.File.ReadAllBytes(filename).ShouldBe(data);
    }

    [Fact]
    public async Task Download_Data_Mediatype()
    {
        var data = "this is a test";
        var uri = new Uri($"data:text/plain,{data}");
        var filename = "testfile";
        var itemId = RandomValues.RandomString(10);
        var metadataFilename = "testmeta";
        var metadata = new MetadataObject('.');

        await foreach (var result in _downloader.DownloadAsync(
            uri,
            filename,
            itemId,
            metadataFilename,
            metadata))
        {
            result.Path.ShouldBe(filename);
            result.Uri.ShouldBe(uri);
            result.ItemId.ShouldBe(itemId);
            result.MetadataPath.ShouldBe(metadataFilename);
        }

        _fileSystem.AllFiles.Count().ShouldBe(2);
        _fileSystem.File.ReadAllBytes(filename).ShouldBe(Encoding.UTF8.GetBytes(data));
    }

    [Fact]
    public async Task Download_Data_Base64()
    {
        var data = Encoding.UTF8.GetBytes("this is a test");
        var uri = new Uri($"data:;base64,{Convert.ToBase64String(data)}");
        var filename = "testfile";
        var itemId = RandomValues.RandomString(10);
        var metadataFilename = "testmeta";
        var metadata = new MetadataObject('.');

        await foreach (var result in _downloader.DownloadAsync(
            uri,
            filename,
            itemId,
            metadataFilename,
            metadata))
        {
            result.Path.ShouldBe(filename);
            result.Uri.ShouldBe(uri);
            result.ItemId.ShouldBe(itemId);
            result.MetadataPath.ShouldBe(metadataFilename);
        }

        _fileSystem.AllFiles.Count().ShouldBe(2);
        _fileSystem.File.ReadAllBytes(filename).ShouldBe(data);
    }

    [Fact]
    public async Task Download_Data()
    {
        var data = "this is a test";
        var uri = new Uri($"data:,{data}");
        var filename = "testfile";
        var itemId = RandomValues.RandomString(10);
        var metadataFilename = "testmeta";
        var metadata = new MetadataObject('.');

        await foreach (var result in _downloader.DownloadAsync(
            uri,
            filename,
            itemId,
            metadataFilename,
            metadata))
        {
            result.Path.ShouldBe(filename);
            result.Uri.ShouldBe(uri);
            result.ItemId.ShouldBe(itemId);
            result.MetadataPath.ShouldBe(metadataFilename);
        }

        _fileSystem.AllFiles.Count().ShouldBe(2);
        _fileSystem.File.ReadAllBytes(filename).ShouldBe(Encoding.UTF8.GetBytes(data));
    }

    [Fact]
    public async Task Download_Data_Large()
    {
        var data = new string('a', 8192);
        var filename = "testfile";
        var itemId = RandomValues.RandomString(10);
        var metadataFilename = "testmeta";
        var metadata = new MetadataObject('.');

        var extractResult = new ExtractResult(
            Encoding.UTF8.GetBytes(data),
            mediaType: "text/plain",
            itemId);

        await foreach (var result in _downloader.DownloadAsync(
            new Uri("data:"),
            filename,
            itemId,
            metadataFilename,
            metadata,
            requestData: extractResult.DownloadRequestData))
        {
            result.Path.ShouldBe(filename);
            result.Uri.AbsoluteUri.ShouldBe("data:");
            result.ItemId.ShouldBe(itemId);
            result.MetadataPath.ShouldBe(metadataFilename);
        }

        _fileSystem.AllFiles.Count().ShouldBe(2);
        _fileSystem.File.ReadAllBytes(filename).ShouldBe(Encoding.UTF8.GetBytes(data));
    }
}
