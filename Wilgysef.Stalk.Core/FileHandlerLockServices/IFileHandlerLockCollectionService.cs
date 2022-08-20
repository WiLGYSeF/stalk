using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.FileHandlerLockServices;

public interface IFileHandlerLockCollectionService : ISingletonDependency
{
    /// <summary>
    /// Gets a file handler lock.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <returns>File handler lock.</returns>
    object GetFileHandlerLock(string path);
}
