using Wilgysef.Stalk.Core.FileHandlerLockServices;
using Wilgysef.Stalk.Core.Shared.Dependencies;
using Wilgysef.Stalk.Core.Shared.FileServices;

namespace Wilgysef.Stalk.Core.ItemIdSetServices;

public class ItemIdSetService : IItemIdSetService, ITransientDependency
{
    private readonly IItemSetCollectionService _itemSetCollectionService;
    private readonly IFileHandlerLockService _fileHandlerLockService;
    private readonly IFileService _fileService;

    public ItemIdSetService(
        IItemSetCollectionService itemSetCollectionService,
        IFileHandlerLockService fileHandlerLockService,
        IFileService fileService)
    {
        _itemSetCollectionService = itemSetCollectionService;
        _fileHandlerLockService = fileHandlerLockService;
        _fileService = fileService;
    }

    public async Task<IItemIdSet> GetItemIdSetAsync(string path, long jobId)
    {
        var itemIds = _itemSetCollectionService.GetItemIdSet(path, jobId);
        if (itemIds == null)
        {
            itemIds = new ItemIdSet();
            try
            {
                using var stream = _fileService.Open(path, FileMode.Open);
                using var reader = new StreamReader(stream);

                for (string? line; (line = await reader.ReadLineAsync()) != null; )
                {
                    if (line.Trim().Length > 0)
                    {
                        itemIds.AddNoPending(line);
                    }
                }
            }
            catch (DirectoryNotFoundException) { }
            catch (FileNotFoundException) { }

            _itemSetCollectionService.AddItemIdSet(path, jobId, itemIds);
        }

        return itemIds;
    }

    public Task<bool> RemoveItemIdSetAsync(long jobId)
    {
        return Task.FromResult(_itemSetCollectionService.RemoveItemIdSet(jobId));
    }

    public Task<bool> RemoveItemIdSetAsync(string path, long jobId)
    {
        return Task.FromResult(_itemSetCollectionService.RemoveItemIdSet(path, jobId));
    }

    public Task<int> WriteChangesAsync(string path, IItemIdSet itemIds)
    {
        lock (_fileHandlerLockService.GetFileHandlerLock(path))
        {
            using var stream = _fileService.Open(path, FileMode.Append);
            using var writer = new StreamWriter(stream);

            foreach (var item in itemIds.PendingItems)
            {
                writer.WriteLine(item);
            }
        }

        return Task.FromResult(itemIds.ResetChangeTracking());
    }
}
