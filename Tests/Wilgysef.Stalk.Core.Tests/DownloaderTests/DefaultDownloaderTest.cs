using Shouldly;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Wilgysef.HttpClientInterception;
using Wilgysef.Stalk.Core.Downloaders;
using Wilgysef.Stalk.Core.MetadataObjects;
using Wilgysef.Stalk.Core.Shared.Downloaders;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;
using Wilgysef.Stalk.TestBase;
using Wilgysef.Stalk.TestBase.Shared.Mocks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Wilgysef.Stalk.Core.Tests.DownloaderTests;

public class DefaultDownloaderTest : BaseTest
{
    private static readonly byte[] TestDownloadData = Encoding.UTF8.GetBytes("test");

    private readonly HttpClientInterceptor _httpInterceptor;
    private readonly HttpRequestEntryLog _httpEntryLog;
    private readonly MockFileSystem _fileSystem;
    private readonly DefaultDownloader _downloader;

    public DefaultDownloaderTest()
    {
        RegisterDownloaders = true;

        var downloaders = GetRequiredService<IEnumerable<IDownloader>>();
        _downloader = (downloaders.Single(d => d is DefaultDownloader) as DefaultDownloader)!;

        _fileSystem = MockFileSystem!;
        _httpInterceptor = HttpClientInterceptor!;
        _httpEntryLog = HttpRequestEntryLog!;

        _httpInterceptor.AddForAny(request =>
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new MemoryStream(TestDownloadData))
            };
        });
    }

    [Fact]
    public void Can_Always_Download()
    {
        _downloader.CanDownload(null!).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Download_File_Save_Metadata(bool computeHash, bool saveFilenameTemplateMetadata)
    {
        var uri = RandomValues.RandomUri();
        var filename = "testfile";
        var itemId = RandomValues.RandomString(10);
        var metadataFilename = "testmeta";
        var metadata = new MetadataObject('.');

        if (!computeHash)
        {
            _downloader.HashName = null;
        }

        _downloader.Config[DownloaderBase.SaveFilenameTemplatesMetadataKey] = saveFilenameTemplateMetadata;

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

        _httpEntryLog.Entries.Count.ShouldBe(1);
        _httpEntryLog.Entries.Single().Request.RequestUri.ShouldBe(uri);

        _fileSystem.AllFiles.Count().ShouldBe(2);
        _fileSystem.File.ReadAllBytes(filename).ShouldBe(TestDownloadData);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        var metadataWritten = new MetadataObject(metadata.KeySeparator);
        metadataWritten.From(deserializer.Deserialize<IDictionary<object, object?>>(
                Encoding.UTF8.GetString(_fileSystem.File.ReadAllBytes(metadataFilename))));

        if (saveFilenameTemplateMetadata)
        {
            metadataWritten.GetValueByParts(MetadataObjectConsts.File.FilenameTemplateKeys).ShouldBe(filename);
            metadataWritten.GetValueByParts(MetadataObjectConsts.MetadataFilenameTemplateKeys).ShouldBe(metadataFilename);
        }
        else
        {
            metadataWritten.ContainsByParts(MetadataObjectConsts.File.FilenameTemplateKeys).ShouldBeFalse();
            metadataWritten.ContainsByParts(MetadataObjectConsts.MetadataFilenameTemplateKeys).ShouldBeFalse();
        }

        metadataWritten.GetValueByParts(MetadataObjectConsts.Origin.ItemIdKeys).ShouldBe(itemId);
        metadataWritten.GetValueByParts(MetadataObjectConsts.Origin.UriKeys).ShouldBe(uri.ToString());
        (DateTime.Now - DateTime.Parse((string)metadataWritten.GetValueByParts(MetadataObjectConsts.RetrievedKeys)!))
            .Duration().TotalMinutes.ShouldBeLessThan(1);
        metadataWritten.GetValueByParts(MetadataObjectConsts.File.SizeKeys).ShouldBe(TestDownloadData.Length.ToString());

        if (computeHash)
        {
            var hashName = "SHA256";
            metadataWritten.GetValueByParts(MetadataObjectConsts.File.HashKeys)
                .ShouldBe(Convert.ToHexString(HashAlgorithm.Create(hashName)!.ComputeHash(TestDownloadData)).ToLower());
            metadataWritten.GetValueByParts(MetadataObjectConsts.File.HashAlgorithmKeys).ShouldBe(hashName);
        }
        else
        {
            metadataWritten.ContainsByParts(MetadataObjectConsts.File.HashKeys).ShouldBeFalse();
            metadataWritten.ContainsByParts(MetadataObjectConsts.File.HashAlgorithmKeys).ShouldBeFalse();
        }
    }

    [Fact]
    public async Task Download_File_DownloadRequestData()
    {
        var uri = RandomValues.RandomUri();
        var filename = "testfile";
        var itemId = RandomValues.RandomString(10);
        var metadataFilename = "testmeta";
        var metadata = new MetadataObject('.');

        var testData = "test data";
        var cookie = "test cookie";

        await foreach (var result in _downloader.DownloadAsync(
            uri,
            filename,
            itemId,
            metadataFilename,
            metadata,
            new DownloadRequestData(
                HttpMethod.Post,
                new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("Set-Cookie", cookie)
                },
                Encoding.UTF8.GetBytes(testData))))
        {
            result.Path.ShouldBe(filename);
            result.Uri.ShouldBe(uri);
            result.ItemId.ShouldBe(itemId);
            result.MetadataPath.ShouldBe(metadataFilename);
        }

        _httpEntryLog.Entries.Count.ShouldBe(1);
        var request = _httpEntryLog.Entries.Single().Request;
        request.RequestUri.ShouldBe(uri);
        request.Method.ShouldBe(HttpMethod.Post);
        request.Headers.GetValues("Set-Cookie").Single().ShouldBe(cookie);
        (await request.Content!.ReadAsStringAsync()).ShouldBe(testData);
    }

    [Fact]
    public async Task Download_File_FormatFilename()
    {
        var uri = RandomValues.RandomUri();
        var filename = "testfile${value}";
        var itemId = RandomValues.RandomString(10);
        var metadataFilename = "testmeta";
        var metadata = new MetadataObject('.');
        metadata["value"] = "abc";

        await foreach (var result in _downloader.DownloadAsync(
            uri,
            filename,
            itemId,
            metadataFilename,
            metadata))
        {
            result.Path.ShouldBe("testfileabc");
            result.Uri.ShouldBe(uri);
            result.ItemId.ShouldBe(itemId);
            result.MetadataPath.ShouldBe(metadataFilename);
        }
    }
}
