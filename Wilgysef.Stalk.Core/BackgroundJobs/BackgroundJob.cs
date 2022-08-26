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

    public virtual bool Abandoned { get; protected set; }

    public virtual string JobArgsName { get; protected set; } = null!;

    public virtual string JobArgs { get; protected set; } = null!;

    public static BackgroundJob Create<T>(
        long id,
        T args,
        int priority = 0,
        DateTime? delayUntil = null,
        TimeSpan? delayFor = null,
        DateTime? maximumLifetime = null,
        TimeSpan? maximumLifespan = null,
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

        if (Priority != priority)
        {
            Priority = priority;
        }
    }

    public void ChangeNextRun(DateTime? nextRun)
    {
        if (Abandoned)
        {
            throw new BackgroundJobAbandonedException();
        }

        if (NextRun != nextRun)
        {
            NextRun = nextRun;
        }
    }

    public void ChangeMaximumLifetime(DateTime? maximumLifetime)
    {
        if (Abandoned)
        {
            throw new BackgroundJobAbandonedException();
        }

        if (MaximumLifetime != maximumLifetime)
        {
            MaximumLifetime = maximumLifetime;
        }
    }

    internal void JobFailed()
    {
        if (Abandoned)
        {
            throw new BackgroundJobAbandonedException();
        }

        Attempts++;

        if (MaximumLifetime.HasValue && MaximumLifetime <= DateTime.Now)
        {
            Abandon();
            return;
        }

        NextRun = DateTime.Now.Add(TimeSpan.FromSeconds(Math.Pow(2, Attempts - 1)));
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

    private static string SerializeArgs<T>(T args) where T : BackgroundJobArgs
    {
        return JsonSerializer.Serialize(args);
    }
}
