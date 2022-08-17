using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.FileHandlerServices;

public interface IFileHandlerService : ISingletonDependency
{
    FileStream GetFileHandler(string path, FileMode fileMode);

    void DecrementUseCount(FileStream stream);
}
