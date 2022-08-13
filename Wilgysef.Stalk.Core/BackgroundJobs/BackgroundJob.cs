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

    public static BackgroundJob Create(
        long id,
        BackgroundJobArgs args,
        int priority = 0,
        DateTime? delayUntil = null,
        TimeSpan? delayFor = null,
        DateTime? maximumLifetime = null,
        TimeSpan? maximumLifespan = null)
    {
        var argsName = args.GetType().FullName;

        if (argsName == null)
        {
            throw new ArgumentNullException(nameof(args), "args does not have FullName.");
        }

        var job = new BackgroundJob
        {
            Id = id,
            JobArgsName = argsName,
            Priority = priority,
            JobArgs = Serialize(args),
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

    internal void SetJobFailed()
    {
        if (Abandoned)
        {
            throw new BackgroundJobAbandonedException();
        }

        Attempts++;

        if (MaximumLifetime.HasValue && MaximumLifetime <= DateTime.Now)
        {
            Abandoned = true;
            NextRun = null;
            return;
        }

        NextRun = DateTime.Now.Add(TimeSpan.FromSeconds(Math.Pow(2, Attempts - 1)));
    }

    internal void SetAbandoned()
    {
        if (!Abandoned)
        {
            Abandoned = true;
        }
    }

    public Type GetJobArgsType()
    {
        var argsType = Type.GetType(JobArgsName);
        if (argsType == null)
        {
            throw new InvalidBackgroundJobException();
        }
        return argsType;
    }

    public BackgroundJobArgs DeserializeArgs()
    {
        var args = JsonSerializer.Deserialize(JobArgs, GetJobArgsType());
        if (args == null)
        {
            throw new InvalidBackgroundJobException();
        }

        return args as BackgroundJobArgs
            ?? throw new InvalidBackgroundJobException();
    }

    private static string Serialize(BackgroundJobArgs args)
    {
        return JsonSerializer.Serialize(args);
    }
}
