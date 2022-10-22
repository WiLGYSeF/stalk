using Wilgysef.Stalk.Core.ObjectInstances;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.FileLockServices;

public class FileLockCollectionService : IFileLockCollectionService, ISingletonDependency
{
    private readonly ObjectInstanceCollection<string, object> _fileLocks = new();

    public IObjectInstanceHandle<object> GetFileLockHandle(string path)
    {
        return _fileLocks.GetHandle(path, () => new object());
    }
}
