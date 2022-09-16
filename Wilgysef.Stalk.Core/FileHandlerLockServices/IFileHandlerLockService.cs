namespace Wilgysef.Stalk.Core.FileHandlerLockServices;

public interface IFileHandlerLockService
{
    object GetFileHandlerLock(string path);
}
