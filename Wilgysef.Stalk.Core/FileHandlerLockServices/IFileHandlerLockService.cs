using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.FileHandlerLockServices;

public interface IFileHandlerLockService : ITransientDependency
{
    object GetFileHandlerLock(string path);
}
