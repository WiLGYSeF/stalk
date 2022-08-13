using Microsoft.EntityFrameworkCore;

namespace Wilgysef.Stalk.Core.Models.JobTasks;

[Owned]
public class JobTaskResult
{
    public virtual bool? Success { get; protected set; }

    public virtual string? ErrorCode { get; protected set; }

    public virtual string? ErrorMessage { get; protected set; }

    public virtual string? ErrorDetail { get; protected set; }

    protected JobTaskResult() { }

    public static JobTaskResult Create(
        bool? success = null,
        string? errorCode = null,
        string? errorMessage = null,
        string? errorDetail = null)
    {
        return new JobTaskResult
        {
            Success = success,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            ErrorDetail = errorDetail,
        };
    }
}
