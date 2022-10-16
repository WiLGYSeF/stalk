using System;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;

namespace Wilgysef.Stalk.Core.Shared.Downloaders
{
    public class DownloadResult
    {
        /// <summary>
        /// Downloaded file path.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Download URI.
        /// </summary>
        public Uri Uri { get; }

        /// <summary>
        /// Item Id.
        /// </summary>
        public string? ItemId { get; }

        /// <summary>
        /// Metadata file path.
        /// </summary>
        public string? MetadataPath { get; }

        /// <summary>
        /// Metadata.
        /// </summary>
        public IMetadataObject? Metadata { get; }

        /// <summary>
        /// Download result.
        /// </summary>
        /// <param name="path">Downloaded file path.</param>
        /// <param name="uri">Download URI.</param>
        /// <param name="itemId">Item Id.</param>
        /// <param name="metadataPath">Metadata file path.</param>
        /// <param name="metadata">Metadata.</param>
        public DownloadResult(
            string path,
            Uri uri,
            string? itemId,
            string? metadataPath = null,
            IMetadataObject? metadata = null)
        {
            Path = path;
            Uri = uri;
            ItemId = itemId;
            MetadataPath = metadataPath;
            Metadata = metadata;
        }
    }
}
