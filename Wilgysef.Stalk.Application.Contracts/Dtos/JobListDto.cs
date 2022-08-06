namespace Wilgysef.Stalk.Application.Contracts.Dtos;

public class JobListDto
{
    public ICollection<JobDto> Jobs { get; set; } = null!;
}
