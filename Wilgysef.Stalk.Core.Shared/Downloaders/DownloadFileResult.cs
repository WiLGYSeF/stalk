namespace Wilgysef.Stalk.Core.Shared.Downloaders
{
    public class DownloadFileResult
    {
        /// <summary>
        /// Downloaded filename.
        /// </summary>
        public string Filename { get; }

        /// <summary>
        /// Metadata filename.
        /// </summary>
        public string? MetadataFilename { get; }

        /// <summary>
        /// Downloaded file size.
        /// </summary>
        public long? FileSize { get; }

        /// <summary>
        /// Downloaded file hash.
        /// </summary>
        public string? Hash { get; }

        /// <summary>
        /// Hash algorithm name.
        /// </summary>
        public string? HashName { get; }

        /// <summary>
        /// Indicates if a metadata file should be created.
        /// </summary>
        public bool CreateMetadata { get; }

        /// <summary>
        /// Download file result.
        /// </summary>
        /// <param name="filename">Download filename.</param>
        /// <param name="metadataFilename">Metadata filename.</param>
        /// <param name="fileSize">Downloaded file size.</param>
        /// <param name="hash">Downloaded file hash.</param>
        /// <param name="hashName">Hash algorithm name.</param>
        /// <param name="createMetadata">Indicates if a metadata file should be created.</param>
        public DownloadFileResult(
            string filename,
            string? metadataFilename,
            long? fileSize,
            string? hash,
            string? hashName,
            bool createMetadata = false)
        {
            Filename = filename;
            MetadataFilename = metadataFilename;
            FileSize = fileSize;
            Hash = hash;
            HashName = hashName;
            CreateMetadata = createMetadata;
        }
    }
}
