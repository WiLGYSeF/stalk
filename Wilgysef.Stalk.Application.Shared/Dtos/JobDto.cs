using Wilgysef.Stalk.Core.Shared.Enums;

namespace Wilgysef.Stalk.Application.Shared.Dtos;

public class JobDto
{
    public long Id { get; set; }

    public string? Name { get; set; }

    public JobState State { get; set; }

    public int Priority { get; set; }

    public DateTime? Started { get; set; }

    public DateTime? Finished { get; set; }

    public string? ConfigJson { get; set; }

    //public ICollection<JobTaskDto> Tasks { get; set; }
}
