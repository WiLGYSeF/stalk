using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Enums;

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

    public virtual string? ConfigJson { get; protected set; }

    public virtual ICollection<JobTask> Tasks { get; protected set; }

    [NotMapped]
    public bool IsActive => State == JobState.Active
        || State == JobState.Cancelling
        || State == JobState.Pausing;

    [NotMapped]
    public bool IsDone => State == JobState.Completed
        || State == JobState.Failed
        || State == JobState.Cancelled;

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

    public void ChangePriority(int priority)
    {
        if (Priority != priority)
        {
            Priority = priority;
        }
    }

    public void ChangeConfig(JobConfig config)
    {
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
        Tasks.Add(task);
    }

    internal void ChangeState(JobState state)
    {
        if (State != state)
        {
            State = state;
        }
    }

    internal void Start()
    {
        Started = DateTime.Now;
    }

    internal void Finish()
    {
        Finished = DateTime.Now;
    }
}
