using Microsoft.EntityFrameworkCore;

namespace Wilgysef.Stalk.Core.Models.JobTasks;

[Owned]
public class JobTaskResult
{
    /// <summary>
    /// <see langword="true"/> if the job task completed successfully, otherwise <see langword="false"/>.
    /// <see langword="null"/> if the job task did not finish.
    /// </summary>
    public virtual bool? Success { get; protected set; }

    /// <summary>
    /// Result error code.
    /// </summary>
    public virtual string? ErrorCode { get; protected set; }

    /// <summary>
    /// Result error message.
    /// </summary>
    public virtual string? ErrorMessage { get; protected set; }

    /// <summary>
    /// Result error detail.
    /// </summary>
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
