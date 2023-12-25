using System;
using System.Text;
using Wilgysef.Stalk.Core.Shared.Downloaders;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;

namespace Wilgysef.Stalk.Core.Shared.Extractors
{
    public class ExtractResult
    {
        public static readonly int DataUriMaxLength = 2048;

        /// <summary>
        /// Extract name.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// Extract priority.
        /// </summary>
        public int Priority { get; }

        /// <summary>
        /// Extract URI.
        /// </summary>
        public Uri Uri { get; }

        /// <summary>
        /// Extract item Id.
        /// </summary>
        public string? ItemId { get; }

        /// <summary>
        /// Extract item data.
        /// </summary>
        public string? ItemData { get; }

        /// <summary>
        /// Extract metadata.
        /// </summary>
        public IMetadataObject? Metadata { get; }

        /// <summary>
        /// Extraction type that should be performed on the URI.
        /// </summary>
        public JobTaskType Type { get; }

        /// <summary>
        /// Download request data.
        /// </summary>
        public DownloadRequestData? DownloadRequestData { get; }

        /// <summary>
        /// Extract result.
        /// </summary>
        /// <param name="uri">Extract URI.</param>
        /// <param name="itemId">Extract item Id.</param>
        /// <param name="type">Extraction type that should be performed on the URI.</param>
        /// <param name="name">Extract name.</param>
        /// <param name="priority">Extract priority.</param>
        /// <param name="itemData">Extract item data.</param>
        /// <param name="metadata">Extract metadata.</param>
        /// <param name="downloadRequestData">Download request data.</param>
        public ExtractResult(
            Uri uri,
            string? itemId,
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

        /// <summary>
        /// Extract result.
        /// </summary>
        /// <param name="uri">Extract URI.</param>
        /// <param name="itemId">Extract item Id.</param>
        /// <param name="type">Extraction type that should be performed on the URI.</param>
        /// <param name="name">Extract name.</param>
        /// <param name="priority">Extract priority.</param>
        /// <param name="itemData">Extract item data.</param>
        /// <param name="metadata">Extract metadata.</param>
        /// <param name="downloadRequestData">Download request data.</param>
        public ExtractResult(
            string uri,
            string? itemId,
            JobTaskType type,
            string? name = null,
            int priority = 0,
            string? itemData = null,
            IMetadataObject? metadata = null,
            DownloadRequestData? downloadRequestData = null)
            : this(
                new Uri(uri),
                itemId,
                type,
                name,
                priority,
                itemData,
                metadata,
                downloadRequestData) { }

        /// <summary>
        /// Extract result.
        /// </summary>
        /// <param name="data">Extract data.</param>
        /// <param name="itemId">Extract item Id.</param>
        /// <param name="name">Extract name.</param>
        /// <param name="priority">Extract priority.</param>
        /// <param name="itemData">Extract item data.</param>
        /// <param name="metadata">Extract metadata.</param>
        /// <param name="downloadRequestData">Download request data.</param>
        public ExtractResult(
            byte[] data,
            string? itemId,
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

        /// <summary>
        /// Extract result.
        /// </summary>
        /// <param name="data">Extract data.</param>
        /// <param name="mediaType">Extract data media type.</param>
        /// <param name="itemId">Extract item Id.</param>
        /// <param name="name">Extract name.</param>
        /// <param name="priority">Extract priority.</param>
        /// <param name="itemData">Extract item data.</param>
        /// <param name="metadata">Extract metadata.</param>
        /// <param name="downloadRequestData">Download request data.</param>
        public ExtractResult(
            byte[] data,
            string mediaType,
            string? itemId,
            string? name = null,
            int priority = 0,
            string? itemData = null,
            IMetadataObject? metadata = null,
            DownloadRequestData? downloadRequestData = null)
            : this(
                new Uri("data:"),
                itemId,
                JobTaskType.Download,
                name,
                priority,
                itemData,
                metadata,
                downloadRequestData)
        {
            var dataUriLength = GetDataUriLength(data.Length, mediaType.Length);
            if (dataUriLength > DataUriMaxLength)
            {
                DownloadRequestData ??= new DownloadRequestData();
                if (DownloadRequestData.Data != null)
                {
                    throw new ArgumentException("Data URI exceeds length and download request data is not null", nameof(data));
                }
                DownloadRequestData.Data = data;
            }
            else
            {
                Uri = new Uri(CreateDataUri(data, mediaType));
            }
        }

        private static string CreateDataUri(byte[] data, string? mediaType = null)
        {
            var expectedLength = GetDataUriLength(data.Length, mediaType?.Length ?? 0);
            var builder = new StringBuilder(expectedLength);
            var addSeparator = true;

            builder.Append("data:");
            if (!string.IsNullOrEmpty(mediaType))
            {
                builder.Append(mediaType);
                if (mediaType[^1] == ';')
                {
                    addSeparator = false;
                }
            }
            if (addSeparator)
            {
                builder.Append(';');
            }
            builder.Append("base64,");
            builder.Append(Convert.ToBase64String(data));

            return builder.ToString();
        }

        private static int GetDataUriLength(int dataLength, int mediaTypeLength)
        {
            return GetBase64Length(dataLength) + mediaTypeLength + 13;
        }

        private static int GetBase64Length(int length)
        {
            return DivideRoundUp(length, 3) * 4;
        }

        private static int DivideRoundUp(int dividend, int divisor)
        {
            var quotient = dividend / divisor;
            var remainder = dividend % divisor;
            return quotient + (remainder != 0 ? 1 : 0);
        }
    }
}
