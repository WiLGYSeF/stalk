using System;

namespace Wilgysef.Stalk.Extractors.YoutubeDl.Core
{
    public interface IDownloadStatus
    {
        /// <summary>
        /// Output downloaded filename.
        /// </summary>
        string? OutputFilename { get; set; }

        /// <summary>
        /// Info JSON metadata filename.
        /// </summary>
        string? MetadataFilename { get; set; }

        /// <summary>
        /// Subtitles filename.
        /// </summary>
        string? SubtitlesFilename { get; set; }

        /// <summary>
        /// Filename of current downloading file.
        /// </summary>
        string? DestinationFilename { get; set; }

        /// <summary>
        /// Download percentage of current downloading file.
        /// </summary>
        double? Percentage { get; set; }

        /// <summary>
        /// File size of current downloading file.
        /// </summary>
        long? TotalSize { get; set; }

        /// <summary>
        /// Average bytes per second of current downloading file.
        /// </summary>
        long? AverageBytesPerSecond { get; set; }

        /// <summary>
        /// Estimated completion time of current downloading file.
        /// </summary>
        TimeSpan? EstimatedCompletionTime { get; set; }
    }
}
