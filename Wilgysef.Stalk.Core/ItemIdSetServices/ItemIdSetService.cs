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

    public async Task<IItemIdSet> GetItemIdSetAsync(string path)
    {
        if (!_itemSetCollectionService.TryGetItemIdSet(path, out var itemIds))
        {
            itemIds = new ItemIdSet();
            try
            {
                using var stream = _fileService.Open(path, FileMode.Open);
                using var reader = new StreamReader(stream);

                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (line.Trim().Length > 0)
                    {
                        itemIds.AddNoPending(line);
                    }
                }
            }
            catch (DirectoryNotFoundException) { }
            catch (FileNotFoundException) { }

            _itemSetCollectionService.AddItemIdSet(path, itemIds);
        }

        return itemIds;
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
