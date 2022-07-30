using Microsoft.EntityFrameworkCore;
using Wilgysef.Stalk.Core.Shared.Enums;

namespace Wilgysef.Stalk.Core.Models.JobTasks;

[Owned]
public class JobTaskResult
{
    public JobTaskResultType Type { get; protected set; }

    public string Uri { get; protected set; }

    public string? MetadataJson { get; protected set; }

    public string? ErrorCode { get; protected set; }

    public string? ErrorMessage { get; protected set; }

    public string? ErrorDetail { get; protected set; }

    public bool IsSuccess => ErrorCode == null;

    protected JobTaskResult() { }

    public JobTaskResult Create(
        JobTaskResultType type,
        string uri,
        string? metadataJson = null,
        string? errorCode = null,
        string? errorMessage = null,
        string? errorDetail = null)
    {
        return new JobTaskResult
        {
            Type = type,
            Uri = uri,
            MetadataJson = metadataJson,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            ErrorDetail = errorDetail,
        };
    }
}
