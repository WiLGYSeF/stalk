namespace Wilgysef.Stalk.Core.FileHandlerServices;

public class FileHandlerService : IFileHandlerService
{
    private Dictionary<string, FileStream> _fileStreams = new();
    private Dictionary<FileStream, int> _handlerKeys = new();

    private readonly object _lock = new();

    public FileStream GetFileHandler(string path, FileMode fileMode)
    {
        lock (_lock)
        {
            if (!_fileStreams.TryGetValue(path, out var stream))
            {
                stream = File.Open(path, fileMode);
                _fileStreams[path] = stream;
                _handlerKeys[stream] = 0;
            }

            _handlerKeys[stream]++;
            return stream;
        }
    }

    public void DecrementUseCount(FileStream stream)
    {
        lock (_lock)
        {
            if (--_handlerKeys[stream] == 0)
            {
                RemoveByValue(_fileStreams, stream);
                _handlerKeys.Remove(stream);
            }
        }
    }

    private static void RemoveByValue<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TValue value)
    {
        var keys = dictionary.Where(pair => object.Equals(pair.Value, value)).ToList();
        foreach (var key in keys)
        {
            dictionary.Remove(key);
        }
    }
}
