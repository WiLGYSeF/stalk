using Wilgysef.Stalk.Shared.Enums;

namespace Wilgysef.Stalk.Core.Models.JobTasks;

public class JobTask
{
    public virtual Guid Id { get; protected set; }

    public virtual string? Name { get; protected set; }

    public virtual JobTaskState State { get; protected set; }

    public virtual int Priority { get; protected set; }

    public virtual string Uri { get; protected set; }

    public virtual JobTaskType? Type { get; protected set; }

    public virtual DateTime? Started { get; protected set; }

    public virtual DateTime? Finished { get; protected set; }

    public virtual JobTaskData Data { get; protected set; }

    public virtual JobTaskResult? Result { get; protected set; }

    public virtual JobTask? ParentTask { get; protected set; }

    protected JobTask() { }

    public JobTask Create(
        string uri,
        string? name = null,
        int priority = 0)
    {
        return new JobTask
        {
            Id = Guid.NewGuid(),
            Name = name,
            State = JobTaskState.Inactive,
            Priority = priority,
            Uri = uri,
        };
    }
}
