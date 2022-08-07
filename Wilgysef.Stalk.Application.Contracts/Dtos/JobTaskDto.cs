using Wilgysef.Stalk.Core.Shared.Enums;

namespace Wilgysef.Stalk.Application.Contracts.Dtos;

public class JobTaskDto
{
    public string Id { get; set; }

    public string? Name { get; set; }

    public string State { get; set; }

    public int Priority { get; set; }

    public string Uri { get; set; }

    public string? ItemId { get; set; }

    public string? ItemData { get; set; }

    public string? MetadataJson { get; set; }

    public string Type { get; set; }

    public DateTime? Started { get; set; }

    public DateTime? Finished { get; set; }

    public DateTime? DelayedUntil { get; set; }

    public JobTaskResultDto Result { get; set; }

    public JobTaskDto? ParentTask { get; set; }
}
