using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Extractors;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;

namespace Wilgysef.Stalk.Core.Models.JobTasks;

public class JobTaskBuilder
{
    public long Id { get; set; }

    public string? Name { get; set; }

    public JobTaskState State { get; set; } = JobTaskState.Inactive;

    public int Priority { get; set; }

    public string? Uri { get; set; }

    public string? ItemId { get; set; }

    public string? ItemData { get; set; }

    public IMetadataObject? Metadata { get; set; }

    public JobTaskType Type { get; set; } = JobTaskType.Extract;

    public DateTime? Started { get; set; }

    public DateTime? Finished { get; set; }

    public DateTime? DelayedUntil { get; set; }

    public JobTaskResult Result { get; set; } = JobTaskResult.Create();

    public long JobId { get; set; }

    public Job? Job { get; set; }

    public long? ParentTaskId { get; set; }

    public JobTask? ParentTask { get; set; }

    public JobTaskBuilder() { }

    public JobTaskBuilder(JobTask task)
    {
        From(task);
    }

    public JobTaskBuilder From(JobTask task)
    {
        Id = task.Id;
        Name = task.Name;
        State = task.State;
        Priority = task.Priority;
        Uri = task.Uri;
        ItemId = task.ItemId;
        ItemData = task.ItemData;
        Metadata = task.GetMetadata().Copy();
        Type = task.Type;
        Started = task.Started;
        Finished = task.Finished;
        DelayedUntil = task.DelayedUntil;
        Result = task.Result;
        JobId = task.JobId;
        Job = task.Job;
        ParentTaskId = task.ParentTaskId;
        ParentTask = task.ParentTask;
        return this;
    }

    public JobTask Create()
    {
        if (Uri == null)
        {
            throw new ArgumentNullException(nameof(Uri));
        }

        return JobTask.Create(
            Id,
            Job!,
            JobId,
            Name,
            State,
            Priority,
            Uri,
            ItemId,
            ItemData,
            Metadata,
            Type,
            Started,
            Finished,
            DelayedUntil,
            Result,
            ParentTaskId,
            ParentTask);
    }

    public JobTaskBuilder WithId(long id)
    {
        Id = id;
        return this;
    }

    public JobTaskBuilder WithName(string? name)
    {
        Name = name;
        return this;
    }

    public JobTaskBuilder WithState(JobTaskState state)
    {
        State = state;
        return this;
    }

    public JobTaskBuilder WithPriority(int priority)
    {
        Priority = priority;
        return this;
    }

    public JobTaskBuilder WithUri(string uri)
    {
        Uri = uri;
        return this;
    }

    public JobTaskBuilder WithItemId(string? itemId)
    {
        ItemId = itemId;
        return this;
    }

    public JobTaskBuilder WithItemData(string? itemData)
    {
        ItemData = itemData;
        return this;
    }

    public JobTaskBuilder WithMetadata(IMetadataObject? metadata)
    {
        Metadata = metadata;
        return this;
    }

    public JobTaskBuilder WithType(JobTaskType type)
    {
        Type = type;
        return this;
    }

    public JobTaskBuilder WithStartedTime(DateTime? started)
    {
        Started = started;
        return this;
    }

    public JobTaskBuilder WithFinishedTime(DateTime? finished)
    {
        Finished = finished;
        return this;
    }

    public JobTaskBuilder WithDelayedUntilTime(DateTime? delayedUntil)
    {
        DelayedUntil = delayedUntil;
        return this;
    }

    public JobTaskBuilder WithDelayTime(TimeSpan delay)
    {
        DelayedUntil = DateTime.Now.Add(delay);
        return this;
    }

    public JobTaskBuilder WithResult(JobTaskResult? result)
    {
        Result = result ?? JobTaskResult.Create();
        return this;
    }

    public JobTaskBuilder WithJobId(long jobId)
    {
        JobId = jobId;
        return this;
    }

    public JobTaskBuilder WithJob(Job job)
    {
        Job = job;
        return this;
    }

    public JobTaskBuilder WithParent(JobTask? parentTask)
    {
        ParentTask = parentTask;
        return this;
    }

    public JobTaskBuilder WithParentId(long parentTaskId)
    {
        ParentTaskId = parentTaskId;
        return this;
    }

    public JobTaskBuilder WithExtractResult(JobTask jobTask, ExtractResult result)
    {
        return WithName(result.Name)
            .WithState(JobTaskState.Inactive)
            .WithUri(result.Uri.AbsoluteUri)
            .WithPriority(result.Priority)
            .WithItemId(result.ItemId)
            .WithItemData(result.ItemData)
            .WithMetadata(result.Metadata)
            .WithType(result.Type)
            .WithJobId(jobTask.JobId)
            .WithParentId(jobTask.Id);
    }

    public JobTaskBuilder WithRetryJobTask(JobTask jobTask)
    {
        return WithName(jobTask.Name)
            .WithState(JobTaskState.Inactive)
            .WithUri(jobTask.Uri)
            .WithPriority(jobTask.Priority)
            .WithItemId(jobTask.ItemId)
            .WithItemData(jobTask.ItemData)
            .WithMetadata(jobTask.GetMetadata().Copy())
            .WithType(jobTask.Type)
            .WithJobId(jobTask.JobId)
            .WithParentId(jobTask.Id);
    }
}
