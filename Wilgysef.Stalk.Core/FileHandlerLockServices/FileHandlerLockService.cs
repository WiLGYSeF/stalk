using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.FileHandlerLockServices;

public class FileHandlerLockService : IFileHandlerLockService, ITransientDependency
{
    private readonly IFileHandlerLockCollectionService _fileHandlerLockCollectionService;

    public FileHandlerLockService(
        IFileHandlerLockCollectionService fileHandlerLockCollectionService)
    {
        _fileHandlerLockCollectionService = fileHandlerLockCollectionService;
    }

    public object GetFileHandlerLock(string path)
    {
        return _fileHandlerLockCollectionService.GetFileHandlerLock(path);
    }
}
