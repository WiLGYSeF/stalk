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

    public DelayConfigDto? Delay { get; set; }

    public class DelayConfigDto
    {
        public RangeDto? TaskDelay { get; set; }

        public RangeDto? TaskFailedDelay { get; set; }

        public RangeDto? TooManyRequestsDelay { get; set; }
    }

    public class RangeDto
    {
        public int Min { get; set; }
        public int Max { get; set; }
    }
}
