namespace Wilgysef.Stalk.Application.Contracts.Dtos;

public class JobListBasicDto
{
    public ICollection<JobBasicDto> Jobs { get; set; } = null!;
}
