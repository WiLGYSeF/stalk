namespace Wilgysef.Stalk.Application.Contracts.Dtos;

public class JobTaskResultDto
{
    public bool Success { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public string? ErrorDetail { get; set; }
}
