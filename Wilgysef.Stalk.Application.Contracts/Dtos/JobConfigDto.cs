namespace Wilgysef.Stalk.Application.Contracts.Dtos;

public class JobConfigDto
{
    public int? MaxTaskWorkerCount { get; set; }

    public string? DownloadFilenameTemplate { get; set; }

    public bool? DownloadData { get; set; }

    public string? MetadataFilenameTemplate { get; set; }

    public bool? SaveMetadata { get; set; }

    public string? ItemIdPath { get; set; }

    public bool? SaveItemIds { get; set; }

    public bool StopWithNoNewItemIds { get; set; }

    public string? LogPath { get; set; }

    public int? LogLevel { get; set; }

    public int? MaxFailures { get; set; }
}
