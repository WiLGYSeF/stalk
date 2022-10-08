using System.IO.Abstractions;
using Wilgysef.Stalk.Core.FileHandlerLockServices;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.ItemIdSetServices;

public class ItemIdSetService : IItemIdSetService, ITransientDependency
{
    private readonly IItemIdSetCollectionService _itemSetCollectionService;
    private readonly IFileHandlerLockService _fileHandlerLockService;
    private readonly IFileSystem _fileSystem;

    public ItemIdSetService(
        IItemIdSetCollectionService itemSetCollectionService,
        IFileHandlerLockService fileHandlerLockService,
        IFileSystem fileSystem)
    {
        _itemSetCollectionService = itemSetCollectionService;
        _fileHandlerLockService = fileHandlerLockService;
        _fileSystem = fileSystem;
    }

    public async Task<IItemIdSet> GetItemIdSetAsync(string path, long jobId)
    {
        var itemIds = _itemSetCollectionService.GetItemIdSet(path, jobId);
        if (itemIds == null)
        {
            itemIds = new ItemIdSet();
            try
            {
                CreateDirectoriesFromFilename(path);

                using var stream = _fileSystem.File.Open(path, FileMode.Open);
                using var reader = new StreamReader(stream);

                for (string? line; (line = await reader.ReadLineAsync()) != null;)
                {
                    line = line.Trim();
                    if (line.Length > 0)
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
            using var stream = _fileSystem.File.Open(path, FileMode.Append);
            using var writer = new StreamWriter(stream);

            foreach (var item in itemIds.PendingItems)
            {
                writer.WriteLine(item);
            }
        }

        return Task.FromResult(itemIds.ResetChangeTracking());
    }

    private void CreateDirectoriesFromFilename(string filename)
    {
        var dirname = Path.GetDirectoryName(filename);
        if (!string.IsNullOrEmpty(dirname))
        {
            _fileSystem.Directory.CreateDirectory(dirname);
        }
    }
}
