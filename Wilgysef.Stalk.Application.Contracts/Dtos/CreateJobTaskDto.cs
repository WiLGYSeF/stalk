namespace Wilgysef.Stalk.Application.Contracts.Dtos;

public class CreateJobTaskDto
{
    public string? Name { get; set; }

    public int Priority { get; set; }

    public string Uri { get; set; }

    public DateTime? DelayedUntil { get; set; }
}
