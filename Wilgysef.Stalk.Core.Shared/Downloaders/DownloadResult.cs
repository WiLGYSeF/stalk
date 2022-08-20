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
    }
}
