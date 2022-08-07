namespace Wilgysef.Stalk.Application.Contracts.Dtos;

public class JobDto
{
    public string Id { get; set; }

    public string? Name { get; set; }

    public string State { get; set; }

    public int Priority { get; set; }

    public DateTime? Started { get; set; }

    public DateTime? Finished { get; set; }

    public DateTime? DelayedUntil { get; set; }

    public JobConfigDto Config { get; set; }

    public ICollection<JobTaskDto> Tasks { get; set; }
}
