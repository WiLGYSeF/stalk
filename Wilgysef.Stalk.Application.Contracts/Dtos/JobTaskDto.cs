using Wilgysef.Stalk.Core.Shared.Enums;

namespace Wilgysef.Stalk.Application.Contracts.Dtos;

public class JobTaskDto
{
    public long Id { get; set; }

    public string? Name { get; set; }

    public JobTaskState State { get; set; }

    public int Priority { get; set; }

    public string Uri { get; set; }

    public string? ItemId { get; set; }

    public string? ItemData { get; set; }

    public string? MetadataJson { get; set; }

    public JobTaskType Type { get; set; }

    public DateTime? Started { get; set; }

    public DateTime? Finished { get; set; }

    public JobTaskResultDto? Result { get; set; }

    public JobTaskDto? ParentTask { get; set; }

    public bool IsActive { get; set; }

    public bool IsDone { get; set; }
}
