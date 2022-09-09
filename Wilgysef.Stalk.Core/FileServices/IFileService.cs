namespace Wilgysef.Stalk.Core.FileServices;

public interface IFileService
{
    Stream Open(string path, FileMode fileMode);
}
