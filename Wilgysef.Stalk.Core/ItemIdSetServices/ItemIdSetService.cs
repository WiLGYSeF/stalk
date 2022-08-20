using Wilgysef.Stalk.Core.FileHandlerLockServices;

namespace Wilgysef.Stalk.Core.ItemIdSetServices;

public class ItemIdSetService : IItemIdSetService
{
    private readonly IFileHandlerLockService _fileHandlerLockService;

    public ItemIdSetService(
        IFileHandlerLockService fileHandlerLockService)
    {
        _fileHandlerLockService = fileHandlerLockService;
    }

    public IItemIdSet GetItemIdSet(string path)
    {
        throw new NotImplementedException();
    }

    public int WriteChanges(string path, IItemIdSet itemIds)
    {
        lock (_fileHandlerLockService.GetFileHandlerLock(path))
        {
            using var stream = File.Open(path, FileMode.Append);
            using var writer = new StreamWriter(stream);
            
            foreach (var item in itemIds.PendingItems)
            {
                writer.WriteLine(item);
            }
        }

        return itemIds.ResetChangeTracking();
    }
}
