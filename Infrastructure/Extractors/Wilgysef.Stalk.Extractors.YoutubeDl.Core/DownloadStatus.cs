using System;

namespace Wilgysef.Stalk.Extractors.YoutubeDl.Core
{
    public class DownloadStatus : IDownloadStatus
    {
        public virtual string? OutputFilename { get; set; }

        public virtual string? MetadataFilename { get; set; }

        public virtual string? SubtitlesFilename { get; set; }

        public virtual string? DestinationFilename { get; set; }

        public virtual double? Percentage { get; set; }

        public virtual long? TotalSize { get; set; }

        public virtual long? AverageBytesPerSecond { get; set; }

        public virtual TimeSpan? EstimatedCompletionTime { get; set; }
    }
}
