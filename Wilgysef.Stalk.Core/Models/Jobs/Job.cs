using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using Wilgysef.Stalk.Core.DomainEvents.Events;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Exceptions;
using Wilgysef.Stalk.Core.Utilities;

namespace Wilgysef.Stalk.Core.Models.Jobs;

public class Job : Entity
{
    /// <summary>
    /// Job Id.
    /// </summary>
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public virtual long Id { get; protected set; }

    /// <summary>
    /// Job name.
    /// </summary>
    public virtual string? Name { get; protected set; }

    /// <summary>
    /// Job state.
    /// </summary>
    public virtual JobState State { get; protected set; }

    /// <summary>
    /// Job priority.
    /// </summary>
    public virtual int Priority { get; protected set; }

    /// <summary>
    /// Job started time.
    /// </summary>
    public virtual DateTime? Started { get; protected set; }

    /// <summary>
    /// Job finished time.
    /// </summary>
    public virtual DateTime? Finished { get; protected set; }

    /// <summary>
    /// Job delayed until time.
    /// </summary>
    public virtual DateTime? DelayedUntil { get; protected set; }

    /// <summary>
    /// Job configuration.
    /// </summary>
    public virtual string? ConfigJson { get; protected set; }

    /// <summary>
    /// Job tasks.
    /// </summary>
    public virtual ICollection<JobTask> Tasks { get; protected set; } = new List<JobTask>();

    /// <summary>
    /// Indicates if the job is active.
    /// </summary>
    [NotMapped]
    public bool IsActive => IsActiveExpression.Compile()(this);

    /// <summary>
    /// Indicates if the job state is transitioning to a different state.
    /// </summary>
    [NotMapped]
    public bool IsTransitioning => IsTransitioningExpression.Compile()(this);

    /// <summary>
    /// Indicates if the job is finished (not cancelled).
    /// </summary>
    [NotMapped]
    public bool IsFinished => IsFinishedExpression.Compile()(this);

    /// <summary>
    /// Indicates if the job is done.
    /// </summary>
    [NotMapped]
    public bool IsDone => IsDoneExpression.Compile()(this);

    /// <summary>
    /// Indicates if the job is queued.
    /// </summary>
    [NotMapped]
    public bool IsQueued => IsQueuedExpression.Compile()(this);

    /// <summary>
    /// Indicates if the job has unfinished tasks.
    /// </summary>
    [NotMapped]
    public bool HasUnfinishedTasks => Tasks.Any(t => !t.IsDone);

    /// <summary>
    /// Indicates if the job has active tasks.
    /// </summary>
    [NotMapped]
    public bool HasActiveTasks => Tasks.Any(t => t.IsActive);

    /// <summary>
    /// Job active expression.
    /// </summary>
    [NotMapped]
    internal static Expression<Func<Job, bool>> IsActiveExpression =>
        j => j.State == JobState.Active
            || j.State == JobState.Cancelling
            || j.State == JobState.Pausing;

    /// <summary>
    /// Job transitioning expression.
    /// </summary>
    [NotMapped]
    internal static Expression<Func<Job, bool>> IsTransitioningExpression =>
        j => j.State == JobState.Cancelling
            || j.State == JobState.Pausing;

    /// <summary>
    /// Job finished expression.
    /// </summary>
    [NotMapped]
    internal static Expression<Func<Job, bool>> IsFinishedExpression =>
        j => j.State == JobState.Completed
            || j.State == JobState.Failed;

    /// <summary>
    /// Job done expression.
    /// </summary>
    [NotMapped]
    internal static Expression<Func<Job, bool>> IsDoneExpression =>
        j => j.State == JobState.Completed
            || j.State == JobState.Failed
            || j.State == JobState.Cancelled;

    /// <summary>
    /// Job queued expression.
    /// </summary>
    [NotMapped]
    internal static Expression<Func<Job, bool>> IsQueuedExpression =>
        j => j.State == JobState.Inactive
            || (j.State == JobState.Paused && j.DelayedUntil.HasValue && j.DelayedUntil.Value < DateTime.Now);

    /// <summary>
    /// Job unfinished tasks expression.
    /// </summary>
    [NotMapped]
    internal static Expression<Func<Job, bool>> HasUnfinishedTasksExpression =>
        j => j.Tasks.AsQueryable().Any(
            Expression.Lambda<Func<JobTask, bool>>(Expression.Negate(JobTask.IsDoneExpression)));

    protected Job() { }

    /// <summary>
    /// Creates a job.
    /// </summary>
    /// <param name="id">Job Id.</param>
    /// <param name="name">Job name.</param>
    /// <param name="priority">Job priority.</param>
    /// <returns>Job.</returns>
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

    internal static Job Create(
        long id,
        string? name,
        JobState state,
        int priority,
        DateTime? started,
        DateTime? finished,
        DateTime? delayedUntil,
        JobConfig? config,
        ICollection<JobTask> tasks)
    {
        if (!started.HasValue && state != JobState.Inactive)
        {
            throw new ArgumentNullException(nameof(started), "Start time cannot be null for a non-inactive job.");
        }

        var job = new Job
        {
            Id = id,
            Name = name,
            State = state,
            Priority = priority,
            Started = started,
            DelayedUntil = delayedUntil,
            Tasks = tasks,
        };

        if (!finished.HasValue && job.IsDone || finished.HasValue && !job.IsDone)
        {
            throw new ArgumentException("Finish time must be set only for a done job.", nameof(finished));
        }

        if (delayedUntil.HasValue && job.State == JobState.Active)
        {
            throw new ArgumentException("Delayed until cannot be set for an active job.", nameof(delayedUntil));
        }

        if (delayedUntil.HasValue && job.State == JobState.Inactive)
        {
            job.ChangeState(JobState.Paused);
        }

        if (finished.HasValue)
        {
            job.Finish(finished.Value);
        }

        job.SetConfig(config);

        return job;
    }

    /// <summary>
    /// Changes job priority.
    /// </summary>
    /// <param name="priority">Job priority.</param>
    public void ChangePriority(int priority)
    {
        if (IsDone)
        {
            throw new JobAlreadyDoneException();
        }

        if (Priority != priority)
        {
            DomainEvents.AddOrReplace(new JobPriorityChangedEvent(Id, Priority, priority));

            Priority = priority;
        }
    }

    /// <summary>
    /// Changes job config.
    /// </summary>
    /// <param name="config">Job config.</param>
    public void ChangeConfig(JobConfig? config)
    {
        if (IsDone)
        {
            throw new JobAlreadyDoneException();
        }

        SetConfig(config);
    }

    /// <summary>
    /// Adds task to job.
    /// </summary>
    /// <param name="task">Job task.</param>
    public void AddTask(JobTask task)
    {
        if (IsDone)
        {
            throw new JobAlreadyDoneException();
        }

        if (!Tasks.Contains(task))
        {
            Tasks.Add(task);
        }
    }

    /// <summary>
    /// Removes job task from job.
    /// </summary>
    /// <param name="task">Job task.</param>
    public void RemoveTask(JobTask task)
    {
        if (IsDone)
        {
            throw new JobAlreadyDoneException();
        }
        if (task.IsActive)
        {
            throw new JobTaskActiveException();
        }

        Tasks.Remove(task);
    }

    /// <summary>
    /// Gets the job config.
    /// </summary>
    /// <returns>Job config.</returns>
    public JobConfig GetConfig()
    {
        if (ConfigJson == null)
        {
            return new JobConfig();
        }

        var config = JsonUtils.TryDeserialize<JobConfig>(
            ConfigJson,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
        if (config == null)
        {
            throw new InvalidOperationException($"{nameof(ConfigJson)} is not valid config.");
        }
        return config;
    }

    public List<JobTask> GetQueuedTasksByPriority()
    {
        return Tasks
            .Where(t => t.IsQueued)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.Started)
            .ToList();
    }

    /// <summary>
    /// Changes the job state.
    /// </summary>
    /// <param name="state">Job state.</param>
    internal void ChangeState(JobState state)
    {
        if (State == state)
        {
            return;
        }

        if (IsDone)
        {
            throw new JobAlreadyDoneException();
        }

        DomainEvents.AddOrReplace(new JobStateChangedEvent(Id, State, state));

        State = state;

        if (IsActive && !Started.HasValue)
        {
            Start();
        }
        else if (IsDone)
        {
            Finish();
        }

        if (IsDone || state == JobState.Active)
        {
            DelayUntil(null);
        }
    }

    /// <summary>
    /// Sets the job config.
    /// </summary>
    /// <param name="config">Job config.</param>
    internal void SetConfig(JobConfig? config)
    {
        config ??= new JobConfig();

        var serialized = Encoding.UTF8.GetString(
            JsonSerializer.SerializeToUtf8Bytes(config, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            }));

        ConfigJson = serialized;
    }

    /// <summary>
    /// Sets the job start time.
    /// </summary>
    /// <param name="dateTime">Start time.</param>
    internal void Start(DateTime? dateTime = null)
    {
        if (IsDone)
        {
            throw new JobAlreadyDoneException();
        }

        if (!Started.HasValue)
        {
            Started = dateTime ?? DateTime.Now;
        }
    }

    /// <summary>
    /// Sets the job finish time.
    /// </summary>
    /// <param name="dateTime">Finish time.</param>
    /// <exception cref="ArgumentException">Finish time is earlier than the start time.</exception>
    internal void Finish(DateTime? dateTime = null)
    {
        if (Finished.HasValue)
        {
            return;
        }

        dateTime ??= DateTime.Now;

        if (Started > dateTime)
        {
            throw new ArgumentException("Finish time cannot be earlier than start time.", nameof(dateTime));
        }

        Finished = dateTime.Value;
        DelayedUntil = null;
    }

    /// <summary>
    /// Sets the job delay until time.
    /// </summary>
    /// <param name="dateTime">Delay until time.</param>
    internal void DelayUntil(DateTime? dateTime)
    {
        if (dateTime.HasValue)
        {
            if (IsDone)
            {
                throw new JobAlreadyDoneException();
            }
            if (IsActive)
            {
                throw new JobActiveException();
            }
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
            case JobState.Active:
                ChangeState(JobState.Inactive);
                break;
            case JobState.Cancelling:
                ChangeState(JobState.Cancelled);
                break;
            case JobState.Pausing:
                ChangeState(JobState.Paused);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Sets the job as done.
    /// </summary>
    /// <exception cref="InvalidOperationException">Job still has active tasks.</exception>
    internal void Done()
    {
        if (HasActiveTasks)
        {
            throw new InvalidOperationException("Job still has active tasks.");
        }

        var failedTaskCount = Tasks.Count(t => t.State == JobTaskState.Failed);
        var config = GetConfig();

        ChangeState((failedTaskCount > config.MaxFailures || failedTaskCount == Tasks.Count(t => t.IsDone))
            ? JobState.Failed
            : JobState.Completed);

        DomainEvents.AddOrReplace(new JobDoneEvent(Id));
    }
}
