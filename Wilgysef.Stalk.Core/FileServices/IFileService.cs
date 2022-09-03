using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.FileServices;

public interface IFileService : ITransientDependency
{
    Stream Open(string path, FileMode fileMode);
}
