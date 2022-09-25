using Shouldly;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Wilgysef.Stalk.Core.Downloaders;
using Wilgysef.Stalk.Core.MetadataObjects;
using Wilgysef.Stalk.Core.Shared.Downloaders;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;
using Wilgysef.Stalk.TestBase;
using Wilgysef.Stalk.TestBase.Mocks;
using Wilgysef.Stalk.TestBase.Shared.Mocks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Wilgysef.Stalk.Core.Tests.DownloaderTests;

public class DefaultDownloaderTest : BaseTest
{
    private static readonly byte[] TestDownloadData = Encoding.UTF8.GetBytes("test");

    private readonly MockHttpMessageHandler _httpMessageHandler;
    private readonly MockFileService _fileService;
    private readonly DefaultDownloader _downloader;

    public DefaultDownloaderTest()
    {
        RegisterDownloaders = true;

        var downloaders = GetRequiredService<IEnumerable<IDownloader>>();
        _downloader = (downloaders.Single(d => d is DefaultDownloader) as DefaultDownloader)!;

        _fileService = MockFileService!;
        _httpMessageHandler = MockHttpMessageHandler!;

        _httpMessageHandler.DefaultEndpointAction = (request, cancellationToken) =>
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new MemoryStream(TestDownloadData))
            });
        };
    }

    [Fact]
    public async Task Download_File_Save_Metadata()
    {
        var uri = RandomValues.RandomUri();
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

        _httpMessageHandler.Requests.Count.ShouldBe(1);
        _httpMessageHandler.Requests.Single().Request.RequestUri.ShouldBe(uri);

        _fileService.Files.Keys.Count().ShouldBe(2);
        (_fileService.Files[filename] as MemoryStream)!.ToArray().ShouldBe(TestDownloadData);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        var metadataWritten = new MetadataObject(metadata.KeySeparator);
        metadataWritten.From(deserializer.Deserialize<IDictionary<object, object?>>(
                Encoding.UTF8.GetString((_fileService.Files[metadataFilename] as MemoryStream)!.ToArray())));

        var hashName = "SHA256";
        metadataWritten.GetValueByParts(MetadataObjectConsts.File.FilenameTemplateKeys).ShouldBe(filename);
        metadataWritten.GetValueByParts(MetadataObjectConsts.MetadataFilenameTemplateKeys).ShouldBe(metadataFilename);
        metadataWritten.GetValueByParts(MetadataObjectConsts.Origin.ItemIdKeys).ShouldBe(itemId);
        metadataWritten.GetValueByParts(MetadataObjectConsts.Origin.UriKeys).ShouldBe(uri.ToString());
        (DateTime.Now - DateTime.Parse((string)metadataWritten.GetValueByParts(MetadataObjectConsts.RetrievedKeys)!))
            .Duration().TotalMinutes.ShouldBeLessThan(1);
        metadataWritten.GetValueByParts(MetadataObjectConsts.File.SizeKeys).ShouldBe(TestDownloadData.Length.ToString());
        metadataWritten.GetValueByParts(MetadataObjectConsts.File.HashKeys)
            .ShouldBe(Convert.ToHexString(HashAlgorithm.Create(hashName)!.ComputeHash(TestDownloadData)).ToLower());
        metadataWritten.GetValueByParts(MetadataObjectConsts.File.HashAlgorithmKeys).ShouldBe(hashName);
    }

    [Fact]
    public async Task Download_File_Save_Metadata_No_Hash()
    {
        var uri = RandomValues.RandomUri();
        var filename = "testfile";
        var itemId = RandomValues.RandomString(10);
        var itemData = RandomValues.RandomString(10);
        var metadataFilename = "testmeta";
        var metadata = new MetadataObject('.');

        _downloader.HashName = null;
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

        _httpMessageHandler.Requests.Count.ShouldBe(1);
        _httpMessageHandler.Requests.Single().Request.RequestUri.ShouldBe(uri);

        _fileService.Files.Keys.Count().ShouldBe(2);
        (_fileService.Files[filename] as MemoryStream)!.ToArray().ShouldBe(TestDownloadData);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        var metadataWritten = new MetadataObject(metadata.KeySeparator);
        metadataWritten.From(deserializer.Deserialize<IDictionary<object, object?>>(
                Encoding.UTF8.GetString((_fileService.Files[metadataFilename] as MemoryStream)!.ToArray())));

        metadataWritten.GetValueByParts(MetadataObjectConsts.File.FilenameTemplateKeys).ShouldBe(filename);
        metadataWritten.GetValueByParts(MetadataObjectConsts.MetadataFilenameTemplateKeys).ShouldBe(metadataFilename);
        metadataWritten.GetValueByParts(MetadataObjectConsts.Origin.ItemIdKeys).ShouldBe(itemId);
        metadataWritten.GetValueByParts(MetadataObjectConsts.Origin.UriKeys).ShouldBe(uri.ToString());
        (DateTime.Now - DateTime.Parse((string)metadataWritten.GetValueByParts(MetadataObjectConsts.RetrievedKeys)!))
            .Duration().TotalMinutes.ShouldBeLessThan(1);
        metadataWritten.GetValueByParts(MetadataObjectConsts.File.SizeKeys).ShouldBe(TestDownloadData.Length.ToString());
    }

    [Fact]
    public void Can_Always_Download()
    {
        _downloader.CanDownload(null!).ShouldBeTrue();
    }
}
