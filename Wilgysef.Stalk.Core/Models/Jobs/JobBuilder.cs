using System.Text.Json;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Enums;

namespace Wilgysef.Stalk.Core.Models.Jobs;

public class JobBuilder
{
    public long Id { get; set; }

    public string? Name { get; set; }

    public JobState State { get; set; }

    public int Priority { get; set; }

    public DateTime? Started { get; set; }

    public DateTime? Finished { get; set; }

    public DateTime? DelayedUntil { get; set; }

    public JobConfig? Config { get; set; }

    public List<JobTask> Tasks { get; set; } = new List<JobTask>();

    public JobBuilder() { }

    public JobBuilder(Job job)
    {
        From(job);
    }

    public JobBuilder From(Job job)
    {
        Id = job.Id;
        Name = job.Name;
        State = job.State;
        Priority = job.Priority;
        Started = job.Started;
        Finished = job.Finished;
        DelayedUntil = job.DelayedUntil;
        Config = job.Config;
        Tasks = job.Tasks.Select(t => new JobTaskBuilder(t).Create()).ToList();
        return this;
    }

    public Job Create()
    {
        return Job.Create(
            Id,
            Name,
            State,
            Priority,
            Started,
            Finished,
            DelayedUntil,
            Config,
            Tasks);
    }

    public JobBuilder WithId(long id)
    {
        Id = id;
        return this;
    }

    public JobBuilder WithName(string? name)
    {
        Name = name;
        return this;
    }

    public JobBuilder WithState(JobState state)
    {
        State = state;
        return this;
    }

    public JobBuilder WithPriority(int priority)
    {
        Priority = priority;
        return this;
    }

    public JobBuilder WithStartedTime(DateTime? started)
    {
        Started = started;
        return this;
    }

    public JobBuilder WithFinishedTime(DateTime? finished)
    {
        Finished = finished;
        return this;
    }

    public JobBuilder WithDelayedUntilTime(DateTime? delayedUntil)
    {
        DelayedUntil = delayedUntil;
        return this;
    }

    public JobBuilder WithConfig(JobConfig config)
    {
        Config = config;
        return this;
    }

    public JobBuilder WithTasks(params JobTask[] tasks)
    {
        Tasks.AddRange(tasks);
        return this;
    }

    public JobBuilder WithNoTasks()
    {
        Tasks.Clear();
        return this;
    }
}
