using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Text.Json;
using Wilgysef.Stalk.Core.Models;

namespace Wilgysef.Stalk.Core.BackgroundJobs;

public class BackgroundJob : Entity
{
    /// <summary>
    /// Background job Id.
    /// </summary>
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public virtual long Id { get; protected set; }

    /// <summary>
    /// Background job priority.
    /// </summary>
    public virtual int Priority { get; protected set; }

    /// <summary>
    /// Times the background job was attempted.
    /// </summary>
    public virtual int Attempts { get; protected set; }

    /// <summary>
    /// Time the background job will be run next.
    /// </summary>
    public virtual DateTime? NextRun { get; protected set; }

    /// <summary>
    /// Maximum lifetime of background job.
    /// </summary>
    public virtual DateTime? MaximumLifetime { get; protected set; }

    /// <summary>
    /// Maximum number of times the background job may be attempted.
    /// </summary>
    public virtual int? MaxAttempts { get; protected set; }

    /// <summary>
    /// Background job state.
    /// </summary>
    public virtual BackgroundJobState State { get; protected set; }

    /// <summary>
    /// Backgroun job arguments type name.
    /// </summary>
    public virtual string JobArgsName { get; protected set; } = null!;

    /// <summary>
    /// Background job arguments.
    /// </summary>
    public virtual string JobArgs { get; protected set; } = null!;

    /// <summary>
    /// Function that determines the next run time on job failure.
    /// </summary>
    [NotMapped]
    public Func<DateTime> GetNextRun { get; set; }

    /// <summary>
    /// Indicates if the background job is scheduled.
    /// </summary>
    [NotMapped]
    public bool IsScheduled => IsScheduledExpression.Compile()(this);

    /// <summary>
    /// Indicates if the background job has been abandoned.
    /// </summary>
    [NotMapped]
    public bool IsAbandoned => IsAbandonedExpression.Compile()(this);

    /// <summary>
    /// Indicates if the background job has succeeded.
    /// </summary>
    [NotMapped]
    public bool IsSucceeded => IsSucceededExpression.Compile()(this);

    /// <summary>
    /// Background job scheduled expression.
    /// </summary>
    [NotMapped]
    internal static Expression<Func<BackgroundJob, bool>> IsScheduledExpression =>
        j => j.State == BackgroundJobState.Scheduled;

    /// <summary>
    /// Background job abandoned expression.
    /// </summary>
    [NotMapped]
    internal static Expression<Func<BackgroundJob, bool>> IsAbandonedExpression =>
        j => j.State == BackgroundJobState.Abandoned;

    /// <summary>
    /// Background job succeeded expression.
    /// </summary>
    [NotMapped]
    internal static Expression<Func<BackgroundJob, bool>> IsSucceededExpression =>
        j => j.State == BackgroundJobState.Succeeded;

    protected BackgroundJob()
    {
        GetNextRun = GetNextRunDefault;
    }

    public static BackgroundJob Create<T>(
        long id,
        T args,
        int priority = 0,
        DateTime? delayUntil = null,
        TimeSpan? delayFor = null,
        DateTime? maximumLifetime = null,
        TimeSpan? maximumLifespan = null,
        int? maximumAttempts = null,
        string? argsName = null) where T : BackgroundJobArgs
    {
        argsName ??= args.GetType().FullName;

        if (argsName == null)
        {
            throw new ArgumentNullException(nameof(argsName));
        }

        var job = new BackgroundJob
        {
            Id = id,
            JobArgsName = argsName,
            Priority = priority,
            MaxAttempts = maximumAttempts,
            JobArgs = SerializeArgs(args),
        };

        if (delayUntil.HasValue)
        {
            job.NextRun = delayUntil.Value;
        }
        else if (delayFor.HasValue)
        {
            job.NextRun = DateTime.Now.Add(delayFor.Value);
        }

        if (maximumLifetime.HasValue)
        {
            job.MaximumLifetime = maximumLifetime.Value;
        }
        else if (maximumLifespan.HasValue)
        {
            job.MaximumLifetime = DateTime.Now.Add(maximumLifespan.Value);
        }

        return job;
    }

    public void ChangePriority(int priority)
    {
        if (IsAbandoned)
        {
            throw new BackgroundJobAbandonedException();
        }

        Priority = priority;
    }

    public void ChangeNextRun(DateTime? nextRun)
    {
        if (IsAbandoned)
        {
            throw new BackgroundJobAbandonedException();
        }

        NextRun = nextRun;
    }

    public void ChangeMaximumLifetime(DateTime? maximumLifetime)
    {
        if (IsAbandoned)
        {
            throw new BackgroundJobAbandonedException();
        }

        MaximumLifetime = maximumLifetime;
    }

    public void ChangeMaxAttempts(int? maxAttempts)
    {
        if (IsAbandoned)
        {
            throw new BackgroundJobAbandonedException();
        }

        MaxAttempts = maxAttempts;
    }

    internal void Success()
    {
        if (IsAbandoned)
        {
            throw new BackgroundJobAbandonedException();
        }

        State = BackgroundJobState.Succeeded;
        Attempts++;
        NextRun = null;
    }

    internal void Failed()
    {
        if (IsAbandoned)
        {
            return;
        }

        Attempts++;

        if (MaxAttempts.HasValue && Attempts >= MaxAttempts.Value
            || MaximumLifetime.HasValue && MaximumLifetime <= DateTime.Now)
        {
            Abandon();
            return;
        }

        NextRun = GetNextRun();
    }

    internal void Abandon()
    {
        if (!IsAbandoned)
        {
            State = BackgroundJobState.Abandoned;
            NextRun = null;
        }
    }

    public Type GetJobArgsType()
    {
        return Type.GetType(JobArgsName)
            ?? throw new InvalidBackgroundJobException();
    }

    public BackgroundJobArgs DeserializeArgs()
    {
        return JsonSerializer.Deserialize(JobArgs, GetJobArgsType()) as BackgroundJobArgs
            ?? throw new InvalidBackgroundJobException();
    }

    public DateTime GetNextRunDefault()
    {
        return DateTime.Now.AddSeconds(Math.Pow(2, Attempts - 1));
    }

    private static string SerializeArgs<T>(T args) where T : BackgroundJobArgs
    {
        return JsonSerializer.Serialize(args);
    }
}
