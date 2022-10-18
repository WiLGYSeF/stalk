using System.IO;
using System.IO.Abstractions;

namespace Wilgysef.Stalk.Extractors.YoutubeDl.Core
{
    public class TemporaryFile : ITemporaryFile
    {
        public string Filename { get; }

        private readonly IFileSystem _fileSystem;

        public TemporaryFile(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;

            Filename = _fileSystem.Path.GetTempFileName();
        }

        public void Dispose()
        {
            try
            {
                _fileSystem.File.Delete(Filename);
            }
            catch (DirectoryNotFoundException) { }
            catch (FileNotFoundException) { }
        }
    }
}
