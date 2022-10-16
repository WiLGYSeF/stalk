namespace Wilgysef.Stalk.Core.FileHandlerLockServices;

public interface IFileHandlerLockService
{
    /// <summary>
    /// Gets a file handler lock.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <returns>File handler lock.</returns>
    object GetFileHandlerLock(string path);
}
