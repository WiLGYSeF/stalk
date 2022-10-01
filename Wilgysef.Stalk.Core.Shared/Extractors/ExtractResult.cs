using System;
using Wilgysef.Stalk.Core.Shared.Downloaders;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;

namespace Wilgysef.Stalk.Core.Shared.Extractors
{
    public class ExtractResult
    {
        public string? Name { get; }

        public int Priority { get; }

        public Uri Uri { get; }

        public string? ItemId { get; }

        public string? ItemData { get; }

        public IMetadataObject? Metadata { get; }

        public JobTaskType Type { get; }

        public DownloadRequestData? DownloadRequestData { get; }

        public ExtractResult(
            Uri uri,
            string itemId,
            JobTaskType type,
            string? name = null,
            int priority = 0,
            string? itemData = null,
            IMetadataObject? metadata = null,
            DownloadRequestData? downloadRequestData = null)
        {
            Name = name;
            Priority = priority;
            Uri = uri;
            ItemId = itemId;
            ItemData = itemData;
            Metadata = metadata;
            Type = type;
            DownloadRequestData = downloadRequestData;
        }

        public ExtractResult(
            byte[] data,
            string itemId,
            JobTaskType type,
            string? name = null,
            int priority = 0,
            string? itemData = null,
            IMetadataObject? metadata = null,
            DownloadRequestData? downloadRequestData = null)
            : this(
                  new Uri("data:;base64," + Convert.ToBase64String(data)),
                  itemId,
                  type,
                  name,
                  priority,
                  itemData,
                  metadata,
                  downloadRequestData) { }
    }
}
