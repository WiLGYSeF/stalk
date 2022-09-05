namespace Wilgysef.Stalk.Core.Shared.Downloaders
{
    public class DownloadFileResult
    {
        public string Filename { get; set; }

        public long FileSize { get; set; }

        public string Hash { get; set; }

        public string HashName { get; set; }

        public DownloadFileResult(
            string filename,
            long fileSize,
            string hash,
            string hashName)
        {
            Filename = filename;
            FileSize = fileSize;
            Hash = hash;
            HashName = hashName;
        }
    }
}
