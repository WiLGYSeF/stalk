using System.Text.Json;
using Wilgysef.Stalk.Core.Shared.Downloaders;
using Wilgysef.Stalk.Core.Shared.Extractors;
using Wilgysef.Stalk.Core.Utilities;

namespace Wilgysef.Stalk.Core.Models.Jobs;

public class JobConfig
{
    public const string GlobalConfigGroupName = "$global";

    public static class ExtractorConfigKeys
    {
        public const string UserAgent = "userAgent";
    }

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
    /// Whether to save <see cref="DownloadFilenameTemplate"/> and <see cref="MetadataFilenameTemplate"/> to metadata.
    /// </summary>
    public bool SaveFilenameTemplatesMetadata { get; set; }

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
    /// Maximum task failures before a job is considered failed.
    /// </summary>
    public int? MaxFailures { get; set; }

    /// <summary>
    /// Logging config.
    /// </summary>
    public Logging? Logs { get; set; }

    /// <summary>
    /// Delay config.
    /// </summary>
    public DelayConfig Delay { get; set; } = new();

    /// <summary>
    /// Extractor configs.
    /// </summary>
    public JobConfigGroupCollection ExtractorConfig { get; set; } = new();

    /// <summary>
    /// Downloader configs.
    /// </summary>
    public JobConfigGroupCollection DownloaderConfig { get; set; } = new();

    /// <summary>
    /// Gets the extractor configurations.
    /// </summary>
    /// <param name="extractor">Extractor.</param>
    /// <returns>Extractor configuration from global config groups and config groups with the same name as <see cref="IExtractor.Name"/>.</returns>
    public Dictionary<string, object?> GetExtractorConfig(IExtractor extractor)
    {
        var config = new Dictionary<string, object?>();
        GetConfig(ExtractorConfig.Where(c => c.Name == GlobalConfigGroupName), config);
        GetConfig(ExtractorConfig.Where(c => c.Name == extractor.Name), config);
        return config;
    }

    /// <summary>
    /// Gets the downloader configurations.
    /// </summary>
    /// <param name="downloader">Downloader.</param>
    /// <returns>Downloader configuration from global config groups and config groups with the same name as <see cref="IDownloader.Name"/>.</returns>
    public Dictionary<string, object?> GetDownloaderConfig(IDownloader downloader)
    {
        var config = new Dictionary<string, object?>
        {
            [DownloaderBase.ConfigKeys.SaveFilenameTemplatesMetadata] = SaveFilenameTemplatesMetadata
        };

        GetConfig(DownloaderConfig.Where(c => c.Name == GlobalConfigGroupName), config);
        GetConfig(DownloaderConfig.Where(c => c.Name == downloader.Name), config);
        return config;
    }

    public static void GetConfig(IEnumerable<JobConfigGroup> configGroups, IDictionary<string, object?> config)
    {
        foreach (var configGroup in configGroups)
        {
            foreach (var (key, val) in configGroup.Config)
            {
                config[key] = val is JsonElement element
                    ? JsonUtils.GetJsonElementValue(element, out _)
                    : val;
            }
        }
    }

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

    public class Range
    {
        public int Min { get; set; }
        public int Max { get; set; }

        public Range(int min, int max)
        {
            Min = min;
            Max = max;
        }
    }
}
