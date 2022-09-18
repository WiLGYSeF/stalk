namespace Wilgysef.Stalk.Core.Models.Jobs;

public class JobConfig
{
    public const string GlobalConfigGroupName = "$global";

    /// <summary>
    /// Maximum concurrent job tasks.
    /// </summary>
    public int MaxTaskWorkerCount { get; set; } = 4;

    /// <summary>
    /// Download filename template.
    /// </summary>
    public string? DownloadFilenameTemplate { get; set; }

    /// <summary>
    /// Whether to download data.
    /// </summary>
    public bool DownloadData { get; set; } = true;

    /// <summary>
    /// Metadata filename template.
    /// </summary>
    public string? MetadataFilenameTemplate { get; set; }

    /// <summary>
    /// Whether to save metadata.
    /// </summary>
    public bool SaveMetadata { get; set; } = true;

    /// <summary>
    /// Item Id path.
    /// </summary>
    public string? ItemIdPath { get; set; }

    /// <summary>
    /// Whether to save item Ids.
    /// </summary>
    public bool SaveItemIds { get; set; } = true;

    /// <summary>
    /// Stop extracting items if all items in the batch are in the item Id set or do not have item Ids.
    /// </summary>
    public bool StopWithNoNewItemIds { get; set; }

    /// <summary>
    /// Logging config.
    /// </summary>
    public Logging? Logs { get; set; }

    /// <summary>
    /// Maximum task failures before a job is considered failed.
    /// </summary>
    public int? MaxFailures { get; set; }

    /// <summary>
    /// Delay config.
    /// </summary>
    public DelayConfig? Delay { get; set; }

    public ICollection<ConfigGroup>? ExtractorConfig { get; set; }

    public ICollection<ConfigGroup>? DownloaderConfig { get; set; }

    public class Logging
    {
        /// <summary>
        /// Log path.
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// Log level.
        /// </summary>
        public int Level { get; set; }
    }

    public class DelayConfig
    {
        /// <summary>
        /// Delay range for subsequent job tasks.
        /// </summary>
        public Range? TaskDelay { get; set; }

        /// <summary>
        /// Delay range for retry job tasks.
        /// </summary>
        public Range? TaskFailedDelay { get; set; }

        /// <summary>
        /// Delay range for retry job tasks for too many requests.
        /// </summary>
        public Range? TooManyRequestsDelay { get; set; }
    }

    public class ConfigGroup
    {
        public string Name { get; set; } = null!;

        public IDictionary<string, object?> Config { get; set; } = null!;
    }

    public class Range
    {
        public int Min { get; set; }
        public int Max { get; set; }
    }
}
