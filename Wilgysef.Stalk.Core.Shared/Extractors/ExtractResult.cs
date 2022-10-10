using System;
using System.Text;
using Wilgysef.Stalk.Core.Shared.Downloaders;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;

namespace Wilgysef.Stalk.Core.Shared.Extractors
{
    public class ExtractResult
    {
        public string? Name { get; }

        public int Priority { get; }

        // Can't use Uri because data URI schemes may exceed the Uri length limit
        public string Uri { get; }

        public string? ItemId { get; }

        public string? ItemData { get; }

        public IMetadataObject? Metadata { get; }

        public JobTaskType Type { get; }

        public DownloadRequestData? DownloadRequestData { get; }

        public ExtractResult(
            string uri,
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
            string? name = null,
            int priority = 0,
            string? itemData = null,
            IMetadataObject? metadata = null,
            DownloadRequestData? downloadRequestData = null)
            : this(
                  data,
                  "",
                  itemId,
                  name,
                  priority,
                  itemData,
                  metadata,
                  downloadRequestData) { }

        public ExtractResult(
            byte[] data,
            string mediaType,
            string itemId,
            string? name = null,
            int priority = 0,
            string? itemData = null,
            IMetadataObject? metadata = null,
            DownloadRequestData? downloadRequestData = null)
            : this(
                  CreateDataUri(data, mediaType),
                  itemId,
                  JobTaskType.Download,
                  name,
                  priority,
                  itemData,
                  metadata,
                  downloadRequestData)
                { }

        private static string CreateDataUri(byte[] data, string? mediaType = null)
        {
            var expectedLength = DivideRoundUp(data.Length, 3) * 4 + (mediaType?.Length ?? 0) + 13;
            var builder = new StringBuilder(expectedLength);
            var addSseparator = true;

            builder.Append("data:");
            if (!string.IsNullOrEmpty(mediaType))
            {
                builder.Append(mediaType);
                if (mediaType[^1] == ';')
                {
                    addSseparator = false;
                }
            }
            if (addSseparator)
            {
                builder.Append(';');
            }
            builder.Append("base64,");
            builder.Append(Convert.ToBase64String(data));

            return builder.ToString();
        }

        private static int DivideRoundUp(int dividend, int divisor)
        {
            var quotient = dividend / divisor;
            var remainder = dividend % divisor;
            return quotient + (remainder != 0 ? 1 : 0);
        }
    }
}
