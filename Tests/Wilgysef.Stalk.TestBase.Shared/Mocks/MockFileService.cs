using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Wilgysef.Stalk.Core.Shared.FileServices;

namespace Wilgysef.Stalk.TestBase.Shared.Mocks
{
    public class MockFileService : IFileService
    {
        public IReadOnlyDictionary<string, Stream> Files => _fileStreams;

        private readonly Dictionary<string, Stream> _fileStreams = new Dictionary<string, Stream>();

        private readonly Dictionary<Regex, Stream> _regexStreams = new Dictionary<Regex, Stream>();

        // TODO: Append read NotSupportedException

        public Stream Open(string path, FileMode fileMode)
        {
            var result = GetStream(path);

            if (result != null)
            {
                switch (fileMode)
                {
                    case FileMode.CreateNew:
                        throw new IOException();
                    case FileMode.Create:
                    case FileMode.Truncate:
                        result = new MemoryStream();
                        break;
                    case FileMode.Append:
                        result.Position = result.Length;
                        break;
                }
            }
            else if (fileMode == FileMode.Open || fileMode == FileMode.Truncate)
            {
                throw new FileNotFoundException();
            }

            result ??= new MemoryStream();
            _fileStreams[path] = result;

            return result;
        }

        public void Delete(string path)
        {
            // TODO
        }

        public void SetFileStream(string path, Stream stream)
        {
            _fileStreams[path] = stream;
        }

        public void SetFileStream(Regex regex, Stream stream)
        {
            _regexStreams[regex] = stream;
        }

        private Stream? GetStream(string path)
        {
            if (_fileStreams.TryGetValue(path, out var existingStream))
            {
                return existingStream;
            }

            foreach (var (regex, stream) in _regexStreams)
            {
                if (regex.IsMatch(path))
                {
                    var result = new MemoryStream();
                    stream.CopyTo(result);
                    return result;
                }
            }

            return null;
        }
    }
}
