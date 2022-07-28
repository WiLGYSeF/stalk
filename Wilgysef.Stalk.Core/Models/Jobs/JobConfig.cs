namespace Wilgysef.Stalk.Core.Models.Jobs;

public class JobConfig
{
    public int MaxTaskWorkerCount { get; set; }

    public string? DestinationPath { get; set; }

    public string? MetadataPath { get; set; }

    public string? ItemIdPath { get; set; }

    public string? LogPath { get; set; }

    public int LogLevel { get; set; }

    public bool ItemDirectories { get; set; }

    public bool ListFull { get; set; }

    public int MaxFailures { get; set; }
}
