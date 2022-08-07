using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
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

    public virtual JobTaskType Type { get; protected set; } = JobTaskType.Extract;

    public virtual DateTime? Started { get; protected set; }

    public virtual DateTime? Finished { get; protected set; }

    public virtual DateTime? DelayedUntil { get; protected set; }

    public virtual JobTaskResult Result { get; protected set; }

    public virtual JobTask? ParentTask { get; protected set; }

    [NotMapped]
    public bool IsActive => IsActiveExpression.Compile()(this);

    [NotMapped]
    public bool IsTransitioning => IsTransitioningExpression.Compile()(this);

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
    internal static Expression<Func<JobTask, bool>> IsTransitioningExpression =>
        t => t.State == JobTaskState.Cancelling
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

    internal static JobTask Create(
        long id,
        string? name,
        JobTaskState state,
        int priority,
        string uri,
        string? itemId,
        string? itemData,
        object? metadata,
        JobTaskType type,
        DateTime? started,
        DateTime? finished,
        DateTime? delayedUntil,
        JobTaskResult? result,
        JobTask? parentTask)
    {
        if (!started.HasValue && state != JobTaskState.Inactive)
        {
            throw new ArgumentNullException(nameof(started), "Start time cannot be null for a non-inactive task.");
        }

        var task = new JobTask
        {
            Id = id,
            Name = name,
            State = state,
            Priority = priority,
            Uri = uri,
            ItemId = itemId,
            ItemData = itemData,
            Type = type,
            Started = started,
            DelayedUntil = delayedUntil,
            Result = result ?? JobTaskResult.Create(),
            ParentTask = parentTask,
        };

        // TODO: itemId, itemData, metadataJson checks

        if (!finished.HasValue && task.IsDone || finished.HasValue && !task.IsDone)
        {
            throw new ArgumentException("Finish time must be set only for a done task.", nameof(finished));
        }

        if (delayedUntil.HasValue && task.State == JobTaskState.Active)
        {
            throw new ArgumentException("Delayed until cannot be set for an active task.", nameof(delayedUntil));
        }

        if (!task.Result.Success.HasValue && task.IsFinished
            || task.Result.Success.HasValue && !task.IsFinished)
        {
            throw new ArgumentException("Result must be set only for a finished task.", nameof(result));
        }
        if (task.State == JobTaskState.Failed && task.Result.Success!.Value
            || task.State == JobTaskState.Completed && !task.Result.Success!.Value)
        {
            throw new ArgumentException("Result success status does not match task state.", nameof(result));
        }

        if (finished.HasValue)
        {
            task.Finish(finished.Value);
        }

        task.SetMetadata(metadata);

        return task;
    }

    // TODO: change to class?
    public void ChangeMetadata(object? metadata)
    {
        if (IsDone)
        {
            throw new JobTaskAlreadyDoneException();
        }

        SetMetadata(metadata);
    }

    internal void ChangeState(JobTaskState state)
    {
        if (IsDone)
        {
            throw new JobTaskAlreadyDoneException();
        }

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

        if (state == JobTaskState.Active)
        {
            DelayUntil(null);
        }
    }

    internal void SetMetadata(object? metadata)
    {
        if (metadata == null)
        {
            if (MetadataJson != null)
            {
                MetadataJson = null;
            }
            return;
        }

        using var jsonDocument = JsonSerializer.SerializeToDocument(metadata, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        if (jsonDocument.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("Metadata must serialize to a JSON object.", nameof(metadata));
        }

        var metadataJson = Encoding.UTF8.GetString(stream.ToArray());

        if (MetadataJson != metadataJson)
        {
            MetadataJson = metadataJson;
        }
    }

    internal void Start(DateTime? dateTime = null)
    {
        if (IsDone)
        {
            throw new JobTaskAlreadyDoneException();
        }

        if (!Started.HasValue)
        {
            Started = dateTime ?? DateTime.Now;
        }
    }

    internal void Finish(DateTime? dateTime = null)
    {
        dateTime ??= DateTime.Now;

        if (Started > dateTime)
        {
            throw new ArgumentException("Finish time cannot be earlier than start time.", nameof(dateTime));
        }

        if (!Finished.HasValue)
        {
            Finished = DateTime.Now;
        }
    }

    internal void DelayUntil(DateTime? dateTime)
    {
        if (IsDone)
        {
            throw new JobTaskAlreadyDoneException();
        }

        DelayedUntil = dateTime;
    }
}
