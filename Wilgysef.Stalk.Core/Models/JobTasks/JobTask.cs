using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Text.Json;
using Wilgysef.Stalk.Core.Exceptions;
using Wilgysef.Stalk.Core.MetadataObjects;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;
using Wilgysef.Stalk.Core.Utilities;

namespace Wilgysef.Stalk.Core.Models.JobTasks;

public class JobTask : Entity
{
    /// <summary>
    /// Job task Id.
    /// </summary>
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public virtual long Id { get; protected set; }

    /// <summary>
    /// Job task name.
    /// </summary>
    public virtual string? Name { get; protected set; }

    /// <summary>
    /// Job task state.
    /// </summary>
    public virtual JobTaskState State { get; protected set; }

    /// <summary>
    /// Job task priority.
    /// </summary>
    public virtual int Priority { get; protected set; }

    /// <summary>
    /// Job task URI.
    /// </summary>
    public virtual string Uri { get; protected set; } = null!;

    /// <summary>
    /// Job task item Id.
    /// </summary>
    public virtual string? ItemId { get; protected set; }

    /// <summary>
    /// Job task item data.
    /// </summary>
    public virtual string? ItemData { get; protected set; }

    /// <summary>
    /// Job task metadata.
    /// </summary>
    public virtual string? MetadataJson { get; protected set; }

    /// <summary>
    /// Job task type.
    /// </summary>
    public virtual JobTaskType Type { get; protected set; } = JobTaskType.Extract;

    /// <summary>
    /// Job task started time.
    /// </summary>
    public virtual DateTime? Started { get; protected set; }

    /// <summary>
    /// Job task finished time.
    /// </summary>
    public virtual DateTime? Finished { get; protected set; }

    /// <summary>
    /// Job task delayed until time.
    /// </summary>
    public virtual DateTime? DelayedUntil { get; protected set; }

    /// <summary>
    /// Download request data.
    /// </summary>
    public virtual JobTaskDownloadRequestData DownloadRequestData { get; protected set; } = null!;

    /// <summary>
    /// Job task result.
    /// </summary>
    public virtual JobTaskResult Result { get; protected set; } = null!;

    /// <summary>
    /// Job foreign key Id.
    /// </summary>
    [ForeignKey(nameof(Job))]
    public virtual long JobId { get; protected set; }

    /// <summary>
    /// Job the job task belongs to.
    /// </summary>
    public virtual Job Job { get; protected set; } = null!;

    /// <summary>
    /// Parent task foreign key Id.
    /// </summary>
    [ForeignKey(nameof(ParentTask))]
    public virtual long? ParentTaskId { get; protected set; }

    /// <summary>
    /// Parent task.
    /// </summary>
    public virtual JobTask? ParentTask { get; protected set; }

    /// <summary>
    /// Indicates if the job task is active.
    /// </summary>
    [NotMapped]
    public bool IsActive => _isActive(this);
    private readonly Func<JobTask, bool> _isActive = IsActiveExpression.Compile();

    /// <summary>
    /// Indicates if the job task is transitioning to a different state.
    /// </summary>
    [NotMapped]
    public bool IsTransitioning => _isTransitioning(this);
    private readonly Func<JobTask, bool> _isTransitioning = IsTransitioningExpression.Compile();

    /// <summary>
    /// Indicates if the job task is finished (not cancelled).
    /// </summary>
    [NotMapped]
    public bool IsFinished => _isFinished(this);
    private readonly Func<JobTask, bool> _isFinished = IsFinishedExpression.Compile();

    /// <summary>
    /// Indicates if the job task is done.
    /// </summary>
    [NotMapped]
    public bool IsDone => _isDone(this);
    private readonly Func<JobTask, bool> _isDone = IsDoneExpression.Compile();

    /// <summary>
    /// Indicates if the job task is queued.
    /// </summary>
    [NotMapped]
    public bool IsQueued => _isQueued(this);
    private readonly Func<JobTask, bool> _isQueued = IsQueuedExpression.Compile();

    /// <summary>
    /// Job task active expression.
    /// </summary>
    [NotMapped]
    internal static Expression<Func<JobTask, bool>> IsActiveExpression =>
        t => t.State == JobTaskState.Active
            || t.State == JobTaskState.Cancelling
            || t.State == JobTaskState.Pausing;

    /// <summary>
    /// Job task transitioning expression.
    /// </summary>
    [NotMapped]
    internal static Expression<Func<JobTask, bool>> IsTransitioningExpression =>
        t => t.State == JobTaskState.Cancelling
            || t.State == JobTaskState.Pausing;

    /// <summary>
    /// Job task finished expression.
    /// </summary>
    [NotMapped]
    internal static Expression<Func<JobTask, bool>> IsFinishedExpression =>
        t => t.State == JobTaskState.Completed
            || t.State == JobTaskState.Failed;

    /// <summary>
    /// Job task done expression.
    /// </summary>
    [NotMapped]
    internal static Expression<Func<JobTask, bool>> IsDoneExpression =>
        t => t.State == JobTaskState.Completed
            || t.State == JobTaskState.Failed
            || t.State == JobTaskState.Cancelled;

    /// <summary>
    /// Job task queued expression.
    /// </summary>
    [NotMapped]
    internal static Expression<Func<JobTask, bool>> IsQueuedExpression =>
        t => t.State == JobTaskState.Inactive
            || (t.State == JobTaskState.Paused && t.DelayedUntil.HasValue && t.DelayedUntil.Value < DateTime.Now);

    protected JobTask() { }

    /// <summary>
    /// Creates a job task.
    /// </summary>
    /// <param name="job">Job the job task belongs to.</param>
    /// <param name="id">Job task Id.</param>
    /// <param name="uri">Job task URI.</param>
    /// <param name="name">Job task name.</param>
    /// <param name="priority">Job task priority.</param>
    /// <param name="type">Job task type.</param>
    /// <returns>Job task.</returns>
    public static JobTask Create(
        long id,
        Job? job,
        long jobId,
        string uri,
        string? name = null,
        int priority = 0,
        JobTaskType type = JobTaskType.Extract)
    {
        return new JobTask
        {
            Id = id,
            Job = job!,
            JobId = jobId,
            Name = name,
            State = JobTaskState.Inactive,
            Priority = priority,
            Uri = uri,
            Type = type,
        };
    }

    internal static JobTask Create(
        long id,
        Job? job,
        long jobId,
        string? name,
        JobTaskState state,
        int priority,
        string uri,
        string? itemId,
        string? itemData,
        IMetadataObject? metadata,
        JobTaskType type,
        DateTime? started,
        DateTime? finished,
        DateTime? delayedUntil,
        JobTaskDownloadRequestData? downloadRequestData,
        JobTaskResult? result,
        long? parentTaskId,
        JobTask? parentTask)
    {
        if (!started.HasValue && state != JobTaskState.Inactive)
        {
            throw new ArgumentNullException(nameof(started), "Start time cannot be null for a non-inactive task.");
        }

        var task = new JobTask
        {
            Id = id,
            Job = job!,
            JobId = jobId,
            Name = name,
            State = state,
            Priority = priority,
            Uri = uri,
            ItemId = itemId,
            ItemData = itemData,
            Type = type,
            Started = started,
            DelayedUntil = delayedUntil,
            DownloadRequestData = downloadRequestData ?? JobTaskDownloadRequestData.Create(),
            Result = result ?? JobTaskResult.Create(),
            ParentTaskId = parentTaskId,
            ParentTask = parentTask,
        };

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

        if (delayedUntil.HasValue && task.State == JobTaskState.Inactive)
        {
            task.ChangeState(JobTaskState.Paused);
        }

        if (finished.HasValue)
        {
            task.Finish(finished.Value);
        }

        task.SetMetadata(metadata);

        return task;
    }

    /// <summary>
    /// Changes the job task metadata.
    /// </summary>
    /// <param name="metadata">Job task metadata.</param>
    public void ChangeMetadata(IMetadataObject? metadata)
    {
        if (IsDone)
        {
            throw new JobTaskAlreadyDoneException();
        }

        SetMetadata(metadata);
    }

    public IMetadataObject GetMetadata()
    {
        if (MetadataJson == null)
        {
            return new MetadataObject();
        }

        var metadataDictionary = JsonUtils.TryDeserializeObject(MetadataJson);
        if (metadataDictionary == null)
        {
            throw new InvalidOperationException("Metadata must be a dictionary.");
        }

        var metadata = new MetadataObject();
        metadata.From(metadataDictionary);

        return metadata;
    }

    /// <summary>
    /// Changes the job task state.
    /// </summary>
    /// <param name="state">Job task state.</param>
    internal void ChangeState(JobTaskState state)
    {
        if (State == state)
        {
            return;
        }

        if (IsDone)
        {
            throw new JobTaskAlreadyDoneException();
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

        if (IsDone || state == JobTaskState.Active)
        {
            DelayUntil(null);
        }
    }

    /// <summary>
    /// Sets the job task metadata.
    /// </summary>
    /// <param name="metadata">Job task metadata.</param>
    /// <exception cref="ArgumentException">Metadata does not serialize to an object.</exception>
    internal void SetMetadata(IMetadataObject? metadata)
    {
        if (metadata == null)
        {
            MetadataJson = null;
            return;
        }

        MetadataJson = JsonSerializer.Serialize(metadata.GetDictionary(), new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });
    }

    /// <summary>
    /// Sets the job task start time.
    /// </summary>
    /// <param name="dateTime">Start time.</param>
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

    /// <summary>
    /// Sets the job task finish time.
    /// </summary>
    /// <param name="dateTime">Finish time.</param>
    /// <exception cref="ArgumentException">Finish time is earlier than the start time.</exception>
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

    /// <summary>
    /// Sets the job task delay until time.
    /// </summary>
    /// <param name="dateTime">Delay until time.</param>
    internal void DelayUntil(DateTime? dateTime)
    {
        if (IsDone && dateTime.HasValue)
        {
            throw new JobTaskAlreadyDoneException();
        }

        DelayedUntil = dateTime;
    }

    /// <summary>
    /// Sets active and transitioning state to their inactive and transitioned states.
    /// </summary>
    internal void Deactivate()
    {
        switch (State)
        {
            case JobTaskState.Active:
                ChangeState(JobTaskState.Inactive);
                break;
            case JobTaskState.Cancelling:
                ChangeState(JobTaskState.Cancelled);
                break;
            case JobTaskState.Pausing:
                ChangeState(JobTaskState.Paused);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Set job task as completed and successful.
    /// </summary>
    internal void Success()
    {
        ChangeState(JobTaskState.Completed);

        Result = JobTaskResult.Create(success: true);
        Finish();
    }

    /// <summary>
    /// Set job task as failed and unsuccessful.
    /// </summary>
    internal void Fail(
        long? retryJobTaskId = null,
        string? errorCode = null,
        string? errorMessage = null,
        string? errorDetail = null)
    {
        ChangeState(JobTaskState.Failed);

        Result = JobTaskResult.Create(
            false,
            retryJobTaskId,
            errorCode,
            errorMessage,
            errorDetail);
    }
}
