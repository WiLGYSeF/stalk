using System.IO;

namespace Wilgysef.Stalk.Core.Shared.FileServices
{
    public interface IFileService
    {
        Stream Open(string path, FileMode fileMode);

        void Delete(string path);
    }
}
