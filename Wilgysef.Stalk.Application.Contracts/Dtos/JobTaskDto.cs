namespace Wilgysef.Stalk.Application.Contracts.Dtos;

public class JobTaskDto
{
    public string Id { get; set; } = null!;

    public string? Name { get; set; }

    public string State { get; set; } = null!;

    public int Priority { get; set; }

    public string Uri { get; set; } = null!;

    public string? ItemId { get; set; }

    public string? ItemData { get; set; }

    public string? MetadataJson { get; set; }

    public string Type { get; set; } = null!;

    public DateTime? Started { get; set; }

    public DateTime? Finished { get; set; }

    public DateTime? DelayedUntil { get; set; }

    public JobTaskResultDto Result { get; set; } = null!;

    public JobTaskDto? ParentTask { get; set; }
}
