using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Enums;

namespace Wilgysef.Stalk.Core.Models.Jobs;

public class Job
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public virtual long Id { get; protected set; }

    public virtual string? Name { get; protected set; }

    public virtual JobState State { get; protected set; }

    public virtual int Priority { get; protected set; }

    public virtual DateTime? Started { get; protected set; }

    public virtual DateTime? Finished { get; protected set; }

    public virtual string? ConfigJson { get; protected set; }

    public virtual ICollection<JobTask> Tasks { get; protected set; } = new List<JobTask>();

    protected Job() { }

    public static Job Create(
        long id,
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

    public void ChangePriority(int priority)
    {
        if (Priority != priority)
        {
            Priority = priority;
        }
    }

    public void Start()
    {
        Started = DateTime.Now;
    }

    public void Finish()
    {
        Finished = DateTime.Now;
    }
}
