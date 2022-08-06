﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Exceptions;

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

    public virtual DateTime? DelayedUntil { get; protected set; }

    public virtual string? ConfigJson { get; protected set; }

    [NotMapped]
    public JobConfig? Config
    {
        get
        {
            if (ConfigJson == null)
            {
                return null;
            }

            var config = JsonSerializer.Deserialize<JobConfig>(ConfigJson);
            if (config == null)
            {
                throw new InvalidOperationException($"{nameof(ConfigJson)} is not valid config.");
            }
            return config;
        }
        set
        {
            ChangeConfig(value);
        }
    }

    public virtual ICollection<JobTask> Tasks { get; protected set; } = new List<JobTask>();

    [NotMapped]
    public bool IsActive => IsActiveExpression.Compile()(this);

    [NotMapped]
    public bool IsFinished => IsFinishedExpression.Compile()(this);

    [NotMapped]
    public bool IsDone => IsDoneExpression.Compile()(this);

    [NotMapped]
    public bool IsQueued => IsQueuedExpression.Compile()(this);

    [NotMapped]
    public bool HasUnfinishedTasks => Tasks.Any(t => !t.IsDone);

    [NotMapped]
    internal static Expression<Func<Job, bool>> IsActiveExpression =>
        j => j.State == JobState.Active
            || j.State == JobState.Cancelling
            || j.State == JobState.Pausing;

    [NotMapped]
    internal static Expression<Func<Job, bool>> IsFinishedExpression =>
        j => j.State == JobState.Completed
            || j.State == JobState.Failed;

    [NotMapped]
    internal static Expression<Func<Job, bool>> IsDoneExpression =>
        j => j.State == JobState.Completed
            || j.State == JobState.Failed
            || j.State == JobState.Cancelled;

    [NotMapped]
    internal static Expression<Func<Job, bool>> IsQueuedExpression =>
        j => j.State == JobState.Inactive;

    [NotMapped]
    internal static Expression<Func<Job, bool>> HasUnfinishedTasksExpression =>
        j => j.Tasks.AsQueryable().Any(
            Expression.Lambda<Func<JobTask, bool>>(Expression.Negate(JobTask.IsDoneExpression)));

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

        if (finished.HasValue)
        {
            job.Finish(finished.Value);
        }

        job.ChangeConfig(config);
        job.DelayUntil(delayedUntil);

        return job;
    }

    public void ChangePriority(int priority)
    {
        if (IsDone)
        {
            throw new JobAlreadyDoneException();
        }

        if (Priority != priority)
        {
            Priority = priority;
        }
    }

    public void ChangeConfig(JobConfig? config)
    {
        if (IsDone)
        {
            throw new JobAlreadyDoneException();
        }

        if (config == null)
        {
            if (ConfigJson != null)
            {
                ConfigJson = null;
            }
            return;
        }

        var serialized = Encoding.UTF8.GetString(
            JsonSerializer.SerializeToUtf8Bytes(config, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            }));

        if (ConfigJson != serialized)
        {
            ConfigJson = serialized;
        }
    }

    public void AddTask(JobTask task)
    {
        if (IsDone)
        {
            throw new JobAlreadyDoneException();
        }

        Tasks.Add(task);
    }

    internal void ChangeState(JobState state)
    {
        if (IsDone)
        {
            throw new JobAlreadyDoneException();
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

        if (state == JobState.Active)
        {
            DelayUntil(null);
        }
    }

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

    internal void Finish(DateTime? dateTime = null)
    {
        dateTime ??= DateTime.Now;

        if (Started > dateTime)
        {
            throw new ArgumentException("Finish time cannot be earlier than start time.", nameof(dateTime));
        }

        if (!Finished.HasValue)
        {
            Finished = dateTime.Value;
        }
    }

    internal void DelayUntil(DateTime? dateTime)
    {
        if (IsDone)
        {
            throw new JobAlreadyDoneException();
        }

        DelayedUntil = dateTime;
    }
}
