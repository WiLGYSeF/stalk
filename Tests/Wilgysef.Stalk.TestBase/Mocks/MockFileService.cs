using System.Text.RegularExpressions;
using Wilgysef.Stalk.Core.FileServices;

namespace Wilgysef.Stalk.TestBase.Mocks;

public class MockFileService : IFileService
{
    public IReadOnlyDictionary<string, Stream> Files => _fileStreams;

    private readonly Dictionary<string, Stream> _fileStreams = new();

    private readonly List<(Regex Regex, Stream Stream)> _regexStreams = new();

    public Stream Open(string path, FileMode fileMode)
    {
        if (_fileStreams.TryGetValue(path, out var existingStream))
        {
            return existingStream;
        }

        Stream? result = null;
        foreach (var (regex, stream) in _regexStreams)
        {
            if (regex.IsMatch(path))
            {
                result = new MemoryStream();
                stream.CopyTo(result);
            }
        }

        result ??= new MemoryStream();
        _fileStreams.Add(path, result);

        return result;
    }

    public void AddFileStream(Regex regex, Stream stream)
    {
        _regexStreams.Add((regex, stream));
    }
}
