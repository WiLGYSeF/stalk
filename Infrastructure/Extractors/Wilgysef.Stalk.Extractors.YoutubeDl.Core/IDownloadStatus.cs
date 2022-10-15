using System;

namespace Wilgysef.Stalk.Extractors.YoutubeDl.Core
{
    public interface IDownloadStatus
    {
        string? OutputFilename { get; set; }

        string? MetadataFilename { get; set; }

        string? SubtitlesFilename { get; set; }

        string? DestinationFilename { get; set; }

        double? Percentage { get; set; }

        long? TotalSize { get; set; }

        long? AverageBytesPerSecond { get; set; }

        TimeSpan? EstimatedCompletionTime { get; set; }
    }
}
