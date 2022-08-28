using System;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;

namespace Wilgysef.Stalk.Core.Shared.Downloaders
{
    public class DownloadResult
    {
        public string Path { get; }

        public Uri Uri { get; }

        public string ItemId { get; }

        public string ItemData { get; }

        public IMetadataObject Metadata { get; }

        public DownloadResult(
            string path,
            Uri uri,
            string itemId = null,
            string itemData = null,
            IMetadataObject metadata = null)
        {
            Path = path;
            Uri = uri;
            ItemId = itemId;
            ItemData = itemData;
            Metadata = metadata;
        }
    }
}
