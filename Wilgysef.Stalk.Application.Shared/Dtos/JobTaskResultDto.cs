namespace Wilgysef.Stalk.Application.Shared.Dtos;

public class JobTaskResultDto
{
    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public string? ErrorDetail { get; set; }

    public bool IsSuccess { get; set; }
}
