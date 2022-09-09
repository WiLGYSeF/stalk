using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.FileHandlerLockServices;

public class FileHandlerLockCollectionService : IFileHandlerLockCollectionService, ISingletonDependency
{
    private readonly Dictionary<string, object> _fileLocks = new();

    private readonly object _lock = new();

    public object GetFileHandlerLock(string path)
    {
        lock (_lock)
        {
            if (!_fileLocks.TryGetValue(path, out var fileLock))
            {
                fileLock = new object();
                _fileLocks[path] = fileLock;
            }
            return fileLock;
        }
    }
}
