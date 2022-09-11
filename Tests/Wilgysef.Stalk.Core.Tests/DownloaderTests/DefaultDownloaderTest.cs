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
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Wilgysef.Stalk.Core.Tests.DownloaderTests;

public class DefaultDownloaderTest : BaseTest
{
    private static byte[] TestDownloadData = Encoding.UTF8.GetBytes("test");

    private readonly HttpRequestMessageLog _requestLog;
    private readonly MockFileService _fileService;
    private readonly DefaultDownloader _downloader;

    public DefaultDownloaderTest()
    {
        RegisterDownloaders = true;

        _requestLog = ReplaceHttpClient(
            (request, cancellationToken) => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new MemoryStream(TestDownloadData))
            });
        _fileService = ReplaceFileService();

        var downloaders = GetRequiredService<IEnumerable<IDownloader>>();
        _downloader = (downloaders.Single(d => d is DefaultDownloader) as DefaultDownloader)!;
    }

    [Fact]
    public async Task Download_File_And_Save_Metadata()
    {
        var uri = new Uri(RandomValues.RandomUri());
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

        _requestLog.RequestEntries.Count.ShouldBe(1);
        _requestLog.RequestEntries.Single().Request.RequestUri.ShouldBe(uri);

        _fileService.Files.Keys.Count().ShouldBe(2);
        (_fileService.Files[filename] as MemoryStream)!.ToArray().ShouldBe(TestDownloadData);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        var metadataWritten = new MetadataObject(metadata.KeySeparator);
        metadataWritten.From(deserializer.Deserialize<IDictionary<object, object>>(
                Encoding.UTF8.GetString((_fileService.Files[metadataFilename] as MemoryStream)!.ToArray())));

        var metadataConsts = new MetadataObjectConsts(metadataWritten.KeySeparator);
        var hashName = "SHA256";
        metadataWritten.GetValue(metadataConsts.FileFilenameTemplateKey).ShouldBe(filename);
        metadataWritten.GetValue(metadataConsts.MetadataFilenameTemplateKey).ShouldBe(metadataFilename);
        metadataWritten.GetValue(metadataConsts.OriginItemIdKey).ShouldBe(itemId);
        metadataWritten.GetValue(metadataConsts.OriginUriKey).ShouldBe(uri.ToString());
        (DateTime.Now - DateTime.Parse((string)metadataWritten.GetValue(metadataConsts.RetrievedKey)))
            .Duration().TotalMinutes.ShouldBeLessThan(1);
        metadataWritten.GetValue(metadataConsts.FileSizeKey).ShouldBe(TestDownloadData.Length.ToString());
        metadataWritten.GetValue(metadataConsts.FileHashKey)
            .ShouldBe(Convert.ToHexString(HashAlgorithm.Create(hashName)!.ComputeHash(TestDownloadData)).ToLower());
        metadataWritten.GetValue(metadataConsts.FileHashAlgorithmKey).ShouldBe(hashName);
    }

    [Fact]
    public void Can_Always_Download()
    {
        _downloader.CanDownload(null).ShouldBeTrue();
    }
}
