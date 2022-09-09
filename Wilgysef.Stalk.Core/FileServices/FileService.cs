using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.FileServices;

public class FileService : IFileService, ITransientDependency
{
    public Stream Open(string path, FileMode fileMode)
    {
        return File.Open(path, fileMode);
    }
}
