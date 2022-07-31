using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wilgysef.Stalk.Core.Models.JobTasks;

[Owned]
public class JobTaskResult
{
    public virtual string? ErrorCode { get; protected set; }

    public virtual string? ErrorMessage { get; protected set; }

    public virtual string? ErrorDetail { get; protected set; }

    [NotMapped]
    public bool IsSuccess => ErrorCode == null;

    protected JobTaskResult() { }

    public JobTaskResult Create(
        string? errorCode = null,
        string? errorMessage = null,
        string? errorDetail = null)
    {
        return new JobTaskResult
        {
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            ErrorDetail = errorDetail,
        };
    }
}
