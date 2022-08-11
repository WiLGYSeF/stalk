using Wilgysef.Stalk.Core.Shared.Cqrs;

namespace Wilgysef.Stalk.Application.Contracts.Commands.JobTasks;

public class CreateJobTask : ICommand
{
    public long JobId { get; }

    public string? Name { get; }

    public int Priority { get; }

    public string Uri { get; }

    public DateTime? DelayedUntil { get; }

    public CreateJobTask(
        long jobId,
        string? name,
        int priority,
        string uri,
        DateTime? delayedUntil)
    {
        JobId = jobId;
        Name = name;
        Priority = priority;
        Uri = uri;
        DelayedUntil = delayedUntil;
    }
}
