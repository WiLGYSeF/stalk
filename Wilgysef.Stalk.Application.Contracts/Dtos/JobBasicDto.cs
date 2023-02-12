namespace Wilgysef.Stalk.Application.Contracts.Dtos;

public class JobBasicDto
{
    public string Id { get; set; } = null!;

    public string? Name { get; set; }

    public string State { get; set; } = null!;

    public int Priority { get; set; }

    public JobConfigDto Config { get; set; } = null!;

    public ICollection<JobTaskBasicDto> Tasks { get; set; } = null!;
}
