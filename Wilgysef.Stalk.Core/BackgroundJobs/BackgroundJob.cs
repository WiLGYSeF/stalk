using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Wilgysef.Stalk.Core.Models;

namespace Wilgysef.Stalk.Core.BackgroundJobs;

public class BackgroundJob : Entity
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public virtual long Id { get; protected set; }

    public virtual int Priority { get; protected set; }

    public virtual int Attempts { get; protected set; }

    public virtual DateTime? NextRun { get; protected set; }

    public virtual DateTime? MaximumLifetime { get; protected set; }

    public virtual int? MaxAttempts { get; protected set; }

    public virtual bool Abandoned { get; protected set; }

    public virtual string JobArgsName { get; protected set; } = null!;

    public virtual string JobArgs { get; protected set; } = null!;

    [NotMapped]
    public Func<DateTime> GetNextRun { get; set; }

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
        if (Abandoned)
        {
            throw new BackgroundJobAbandonedException();
        }

        Priority = priority;
    }

    public void ChangeNextRun(DateTime? nextRun)
    {
        if (Abandoned)
        {
            throw new BackgroundJobAbandonedException();
        }

        NextRun = nextRun;
    }

    public void ChangeMaximumLifetime(DateTime? maximumLifetime)
    {
        if (Abandoned)
        {
            throw new BackgroundJobAbandonedException();
        }

        MaximumLifetime = maximumLifetime;
    }

    public void ChangeMaxAttempts(int? maxAttempts)
    {
        if (Abandoned)
        {
            throw new BackgroundJobAbandonedException();
        }

        MaxAttempts = maxAttempts;
    }

    internal void JobFailed()
    {
        if (Abandoned)
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
        if (!Abandoned)
        {
            Abandoned = true;
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
