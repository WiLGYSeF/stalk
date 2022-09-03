namespace Wilgysef.Stalk.Core.Shared.Downloaders
{
    public class DownloadFileResult
    {
        public long FileSize { get; }

        public string Hash { get; }

        public DownloadFileResult(
            long fileSize,
            string hash)
        {
            FileSize = fileSize;
            Hash = hash;
        }
    }
}
