using Moq;
using Shouldly;
using System.Text;
using Wilgysef.Stalk.Core.ItemIdSetServices;
using Wilgysef.Stalk.Core.Shared.FileServices;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Core.Tests.ItemIdSetServiceTests;

public class ItemIdSetServiceTest : BaseTest
{
    private Mock<IFileService> _fileService = null!;
    private IItemIdSetService _itemIdSetService = null!;

    private readonly string[] TestData = new[]
    {
        "testid1",
        "testid2",
        "testid3",
        "testid4",
    };

    [Fact]
    public async Task Get_ItemIdSet_New()
    {
        Setup(false);

        var itemIds = await _itemIdSetService.GetItemIdSetAsync("abc", 0);
        itemIds.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Get_ItemIdSet_Existing()
    {
        Setup(false);

        var itemIds = await _itemIdSetService.GetItemIdSetAsync("abc", 0);
        itemIds.Add("test1");
        itemIds.Add("test2");

        var itemIds2 = await _itemIdSetService.GetItemIdSetAsync("abc", 0);
        itemIds2.ShouldBe(itemIds);
    }

    [Fact]
    public async Task Get_ItemIdSet_From_Disk()
    {
        Setup(true);

        var itemIds = await _itemIdSetService.GetItemIdSetAsync("abc", 0);

        itemIds.Count.ShouldBe(TestData.Length);
        foreach (var id in TestData)
        {
            itemIds.Contains(id).ShouldBeTrue();
        }
    }

    [Fact]
    public async Task Write_Changes()
    {
        using var stream = GetTestDataAsStream();
        Setup(true, stream);

        var newIds = new[]
        {
            "asdf",
            "1234",
        };

        var path = "abc";
        var itemIds = await _itemIdSetService.GetItemIdSetAsync(path, 0);

        foreach (var id in newIds)
        {
            itemIds.Add(id);
        }

        itemIds.Count.ShouldBe(TestData.Length + 2);

        await _itemIdSetService.WriteChangesAsync(path, itemIds);

        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var contents = await reader.ReadToEndAsync();

        contents.ShouldBe(
            string.Join(Environment.NewLine, TestData.Concat(newIds)) + Environment.NewLine);
    }

    [Fact]
    public void Remove_When_NoReferences()
    {
        var itemIdSetCollectionService = GetRequiredService<IItemIdSetCollectionService>();

        var path = "abc";
        itemIdSetCollectionService.AddItemIdSet(path, 0, new ItemIdSet());
        itemIdSetCollectionService.AddItemIdSet(path, 1, new ItemIdSet());

        itemIdSetCollectionService.GetItemIdSet(path, 1)
            .ShouldBe(itemIdSetCollectionService.GetItemIdSet(path, 0));

        itemIdSetCollectionService.RemoveItemIdSet(path, 1);
        itemIdSetCollectionService.GetItemIdSet(path, 0).ShouldNotBeNull();
        itemIdSetCollectionService.RemoveItemIdSet(path, 0);
        itemIdSetCollectionService.GetItemIdSet(path, 0).ShouldBeNull();
    }

    [Fact]
    public void Remove_ItemIdSet()
    {
        var itemIdSetCollectionService = GetRequiredService<IItemIdSetCollectionService>();

        var path = "abc";
        itemIdSetCollectionService.AddItemIdSet(path, 0, new ItemIdSet());

        itemIdSetCollectionService.RemoveItemIdSet(path, 0).ShouldBeTrue();
        itemIdSetCollectionService.RemoveItemIdSet(path, 0).ShouldBeFalse();

        itemIdSetCollectionService.GetItemIdSet(path, 0).ShouldBeNull();
    }

    [Fact]
    public void Remove_ItemIdSet_JobId()
    {
        var itemIdSetCollectionService = GetRequiredService<IItemIdSetCollectionService>();

        var path1 = "abc";
        var path2 = "def";
        itemIdSetCollectionService.AddItemIdSet(path1, 0, new ItemIdSet());
        itemIdSetCollectionService.AddItemIdSet(path2, 0, new ItemIdSet());

        itemIdSetCollectionService.RemoveItemIdSet(0).ShouldBeTrue();
        itemIdSetCollectionService.RemoveItemIdSet(0).ShouldBeFalse();

        itemIdSetCollectionService.GetItemIdSet(path1, 0).ShouldBeNull();
    }

    private void Setup(bool fileExists)
    {
        Setup(fileExists, GetTestDataAsStream());
    }

    private void Setup(bool fileExists, Stream stream)
    {
        _fileService = new Mock<IFileService>();
        _fileService.Setup(m => m.Open(It.IsAny<string>(), It.IsAny<FileMode>()))
            .Returns(GetStream);

        ReplaceServiceInstance(_fileService.Object);

        _itemIdSetService = GetRequiredService<IItemIdSetService>();

        Stream GetStream(string path, FileMode fileMode)
        {
            if (!fileExists)
            {
                throw new FileNotFoundException();
            }
            return stream;
        }
    }

    private Stream GetTestDataAsStream()
    {
        var stream = new MemoryStreamNonDisposable();
        var buffer = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, TestData) + Environment.NewLine);
        stream.Write(buffer, 0, buffer.Length);
        stream.Position -= buffer.Length;
        return stream;
    }

    private class MemoryStreamNonDisposable : MemoryStream
    {
        public MemoryStreamNonDisposable() : base() { }

        public MemoryStreamNonDisposable(byte[] buffer) : base(buffer) { }

        protected override void Dispose(bool disposing)
        {
            // do not dispose
        }

        public void ManualDispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
