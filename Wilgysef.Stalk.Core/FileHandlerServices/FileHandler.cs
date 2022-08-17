namespace Wilgysef.Stalk.Core.FileHandlerServices;

public class FileHandler : IDisposable
{
    // TODO: require locking before getting stream
    public FileStream Stream { get; private set; }

    public object Lock { get; } = new();

    private readonly IFileHandlerService _fileHandlerService;

    internal FileHandler(
        IFileHandlerService fileHandlerService,
        string path,
        FileMode fileMode)
    {
        _fileHandlerService = fileHandlerService;
        Stream = File.Open(path, fileMode);
    }

    public void Dispose()
    {
        _fileHandlerService.DecrementUseCount(Stream);

        GC.SuppressFinalize(this);
    }
}
