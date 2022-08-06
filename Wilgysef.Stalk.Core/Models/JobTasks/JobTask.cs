using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Exceptions;

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

    public virtual DateTime? DelayedUntil { get; protected set; }

    public virtual JobTaskResult? Result { get; protected set; }

    public virtual JobTask? ParentTask { get; protected set; }

    [NotMapped]
    public bool IsActive => IsActiveExpression.Compile()(this);

    [NotMapped]
    public bool IsFinished => IsFinishedExpression.Compile()(this);

    [NotMapped]
    public bool IsDone => IsDoneExpression.Compile()(this);

    [NotMapped]
    public bool IsQueued => IsQueuedExpression.Compile()(this);

    [NotMapped]
    internal static Expression<Func<JobTask, bool>> IsActiveExpression =>
        t => t.State == JobTaskState.Active
            || t.State == JobTaskState.Cancelling
            || t.State == JobTaskState.Pausing;

    [NotMapped]
    internal static Expression<Func<JobTask, bool>> IsFinishedExpression =>
        t => t.State == JobTaskState.Completed
            || t.State == JobTaskState.Failed;

    [NotMapped]
    internal static Expression<Func<JobTask, bool>> IsDoneExpression =>
        t => t.State == JobTaskState.Completed
            || t.State == JobTaskState.Failed
            || t.State == JobTaskState.Cancelled;

    [NotMapped]
    internal static Expression<Func<JobTask, bool>> IsQueuedExpression =>
        j => j.State == JobTaskState.Inactive;

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
        if (State == state)
        {
            return;
        }

        State = state;

        if (IsActive && !Started.HasValue)
        {
            Start();
        }
        else if (IsDone)
        {
            Finish();
        }

        if (state != JobTaskState.Paused)
        {
            DelayUntil(null);
        }
    }

    internal void Start()
    {
        if (IsDone)
        {
            throw new JobTaskAlreadyDoneException();
        }

        Started = DateTime.Now;
    }

    internal void Finish()
    {
        if (!Finished.HasValue)
        {
            Finished = DateTime.Now;
        }
    }

    internal void DelayUntil(DateTime? dateTime)
    {
        DelayedUntil = dateTime;
    }
}
