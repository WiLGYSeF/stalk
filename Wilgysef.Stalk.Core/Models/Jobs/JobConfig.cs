namespace Wilgysef.Stalk.Core.Models.Jobs;

public class JobConfig
{
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
    /// Log path.
    /// </summary>
    public string? LogPath { get; set; }

    /// <summary>
    /// Log level.
    /// </summary>
    public int LogLevel { get; set; }

    /// <summary>
    /// Maximum task failures before a job is considered failed.
    /// </summary>
    public int? MaxFailures { get; set; }
}
