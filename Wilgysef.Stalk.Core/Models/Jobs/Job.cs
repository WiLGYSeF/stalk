using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Shared.Enums;

namespace Wilgysef.Stalk.Core.Models.Jobs;

public class Job
{
    public virtual Guid Id { get; protected set; }

    public virtual string? Name { get; protected set; }

    public virtual JobState State { get; protected set; }

    public virtual int Priority { get; protected set; }

    public virtual DateTime? Started { get; protected set; }

    public virtual DateTime? Finished { get; protected set; }

    public virtual JobConfig Config { get; protected set; } = new();

    public virtual ICollection<JobTask> Tasks { get; protected set; } = new List<JobTask>();

    protected Job() { }

    public Job Create(
        Guid id,
        string? name = null,
        int priority = 0)
    {
        return new Job
        {
            Id = id,
            Name = name,
            State = JobState.Inactive,
            Priority = priority,
        };
    }
}
