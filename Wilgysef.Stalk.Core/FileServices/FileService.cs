namespace Wilgysef.Stalk.Core.FileServices;

public class FileService : IFileService
{
    public Stream Open(string path, FileMode fileMode)
    {
        return File.Open(path, fileMode);
    }
}
