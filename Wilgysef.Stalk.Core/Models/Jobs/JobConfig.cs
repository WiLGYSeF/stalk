namespace Wilgysef.Stalk.Core.Models.Jobs;

public class JobConfig
{
    public int MaxTaskWorkerCount { get; set; } = 4;

    public string? DestinationPath { get; set; }

    public bool DownloadData { get; set; } = true;

    public string? MetadataPath { get; set; }

    public bool SaveMetadata { get; set; } = true;

    public string? ItemIdPath { get; set; }

    public bool SaveItemIds { get; set; } = true;

    public string? LogPath { get; set; }

    public int LogLevel { get; set; }

    public bool ItemDirectories { get; set; }

    public bool ListFull { get; set; }

    public int MaxFailures { get; set; } = 10;
}
