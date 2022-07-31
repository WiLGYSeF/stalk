using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Wilgysef.Stalk.Core.Shared.Enums;

namespace Wilgysef.Stalk.Core.Models.JobTasks;

public class JobTask
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public virtual long Id { get; protected set; }

    public virtual string? Name { get; protected set; }

    public virtual JobTaskState State { get; protected set; }

    public virtual int Priority { get; protected set; }

    public virtual string Uri { get; protected set; }

    public virtual string? ItemId { get; protected set; }

    public virtual string? ItemData { get; protected set; }

    public virtual string? MetadataJson { get; protected set; }

    public virtual JobTaskType Type { get; protected set; }

    public virtual DateTime? Started { get; protected set; }

    public virtual DateTime? Finished { get; protected set; }

    public virtual JobTaskResult? Result { get; protected set; }

    public virtual JobTask? ParentTask { get; protected set; }

    [NotMapped]
    public bool IsActive => State == JobTaskState.Active
        || State == JobTaskState.Cancelling
        || State == JobTaskState.Pausing;

    [NotMapped]
    public bool IsDone => State == JobTaskState.Completed
        || State == JobTaskState.Failed
        || State == JobTaskState.Cancelled;

    protected JobTask() { }

    public static JobTask Create(
        long id,
        string uri,
        string? name = null,
        int priority = 0,
        JobTaskType type = JobTaskType.Extract)
    {
        return new JobTask
        {
            Id = id,
            Name = name,
            State = JobTaskState.Inactive,
            Priority = priority,
            Uri = uri,
            Type = type,
        };
    }

    internal void ChangeState(JobTaskState state)
    {
        if (State != state)
        {
            State = state;
        }
    }

    internal void Start()
    {
        Started = DateTime.Now;
    }

    internal void Finish()
    {
        Finished = DateTime.Now;
    }
}
