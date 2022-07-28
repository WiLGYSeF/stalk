namespace Wilgysef.Stalk.Core.Models.JobTasks;

public class JobTaskResult
{
    public string? ErrorCode { get; protected set; }

    public string? Message { get; protected set; }

    public string? Detail { get; protected set; }

    public bool IsSuccess => ErrorCode == null;

    protected JobTaskResult() { }

    public JobTaskResult Create(string? errorCode = null, string? message = null, string? detail = null)
    {
        return new JobTaskResult
        {
            ErrorCode = errorCode,
            Message = message,
            Detail = detail,
        };
    }
}
