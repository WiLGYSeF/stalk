using Wilgysef.Stalk.Core.ObjectInstances;

namespace Wilgysef.Stalk.Core.FileLockServices;

public interface IFileLockCollectionService
{
    /// <summary>
    /// Gets a file handler lock.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <returns>File handler lock handle.</returns>
    IObjectInstanceHandle<object> GetFileLockHandle(string path);
}
