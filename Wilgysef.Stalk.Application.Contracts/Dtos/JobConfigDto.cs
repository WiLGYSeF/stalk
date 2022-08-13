namespace Wilgysef.Stalk.Application.Contracts.Dtos;

public class JobConfigDto
{
    public int? MaxTaskWorkerCount { get; set; }

    public string? DestinationPath { get; set; }

    public bool? DownloadData { get; set; }

    public string? MetadataPath { get; set; }

    public bool? SaveMetadata { get; set; }
    public string? ItemIdPath { get; set; }

    public bool? SaveItemIds { get; set; }

    public string? LogPath { get; set; }

    public int? LogLevel { get; set; }

    public bool? ItemDirectories { get; set; }

    public bool? ListFull { get; set; }

    public int? MaxFailures { get; set; }
}
