namespace Wilgysef.Stalk.Application.Contracts.Dtos;

public class JobTaskBasicDto
{
    public string Id { get; set; } = null!;

    public string? Name { get; set; }

    public string State { get; set; } = null!;

    public int Priority { get; set; }
}
