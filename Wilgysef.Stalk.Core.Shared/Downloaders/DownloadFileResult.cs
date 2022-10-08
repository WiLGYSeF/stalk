namespace Wilgysef.Stalk.Core.Shared.Downloaders
{
    public class DownloadFileResult
    {
        public string Filename { get; }

        public string? MetadataFilename { get; }

        public long? FileSize { get; }

        public string? Hash { get; }

        public string? HashName { get; }

        public bool CreateMetadata { get; }

        public DownloadFileResult(
            string filename,
            string? metadataFilename,
            long? fileSize,
            string? hash,
            string? hashName,
            bool createMetadata = false)
        {
            if (!createMetadata && metadataFilename != null)
            {
                createMetadata = true;
            }

            Filename = filename;
            MetadataFilename = metadataFilename;
            FileSize = fileSize;
            Hash = hash;
            HashName = hashName;
            CreateMetadata = createMetadata;
        }
    }
}
