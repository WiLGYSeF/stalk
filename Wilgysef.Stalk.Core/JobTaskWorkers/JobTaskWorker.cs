using IdGen;
using Wilgysef.Stalk.Core.Downloaders;
using Wilgysef.Stalk.Core.ItemIdSetServices;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared;
using Wilgysef.Stalk.Core.Shared.Downloaders;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Extractors;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Core.JobTaskWorkers;

public class JobTaskWorker : IJobTaskWorker
{
    public Job? Job { get; protected set; }

    public JobTask? JobTask { get; protected set; }

    private JobConfig JobConfig { get; set; }

    private readonly IServiceLocator _serviceLocator;

    public JobTaskWorker(
        IServiceLocator serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    public IJobTaskWorker WithJobTask(Job job, JobTask jobTask)
    {
        Job = job;
        JobTask = jobTask;
        return this;
    }

    public async Task WorkAsync(CancellationToken cancellationToken = default)
    {
        if (Job == null)
        {
            throw new InvalidOperationException("Job is not set.");
        }
        if (JobTask == null)
        {
            throw new InvalidOperationException("Job task is not set.");
        }

        try
        {
            JobConfig = Job.GetConfig();

            switch (JobTask.Type)
            {
                case JobTaskType.Extract:
                    await ExtractAsync(cancellationToken);
                    break;
                case JobTaskType.Download:
                    await DownloadAsync(cancellationToken);
                    break;
                default:
                    throw new NotImplementedException();
            }

            JobTask.ChangeState(JobTaskState.Completed);
        }
        finally
        {
            using var scope = _serviceLocator.BeginLifetimeScope();
            var jobManager = scope.GetRequiredService<IJobManager>();

            JobTask.Deactivate();
            await jobManager.UpdateJobAsync(Job, CancellationToken.None);
        }
    }

    protected virtual async Task ExtractAsync(CancellationToken cancellationToken)
    {
        var jobTaskUri = new Uri(JobTask!.Uri);

        IExtractor? extractor = null;
        using (var scope = _serviceLocator.BeginLifetimeScope())
        {
            extractor = scope.GetRequiredService<IEnumerable<IExtractor>>()
                .FirstOrDefault(e => e.CanExtract(jobTaskUri));
        }

        if (extractor == null)
        {
            throw new JobTaskWorkerException(
                StalkErrorCodes.JobTaskWorkerNoExtractor,
                "No extractor found.",
                $"No extractor was able to extract from {jobTaskUri}");
        }

        using (var scope = _serviceLocator.BeginLifetimeScope())
        {
            var idGenerator = scope.GetRequiredService<IIdGenerator<long>>();

            await foreach (var result in extractor.ExtractAsync(jobTaskUri, JobTask.ItemData, JobTask.GetMetadata(), cancellationToken))
            {
                Job!.AddTask(new JobTaskBuilder()
                    .WithId(idGenerator.CreateId())
                    .WithExtractResult(JobTask, result)
                    .Create());
            }

            var jobManager = scope.GetRequiredService<IJobManager>();
            await jobManager.UpdateJobAsync(Job!, CancellationToken.None);
        }
    }

    protected virtual async Task DownloadAsync(CancellationToken cancellationToken)
    {
        var jobTaskUri = new Uri(JobTask!.Uri);

        IDownloader? downloader = null;
        IItemIdSet? itemIds = null;

        using (var scope = _serviceLocator.BeginLifetimeScope())
        {
            var downloaders = scope.GetRequiredService<IEnumerable<IDownloader>>();
            var defaultDownloader = downloaders.First(d => d is IDefaultDownloader);
            downloader = scope.GetRequiredService<IEnumerable<IDownloader>>()
                .FirstOrDefault(d => d is not IDefaultDownloader && d.CanDownload(jobTaskUri))
                ?? defaultDownloader;

            var itemIdSetService = scope.GetRequiredService<IItemIdSetService>();
            if (JobConfig.SaveItemIds && JobConfig.ItemIdPath != null)
            {
                itemIds = itemIdSetService.GetItemIdSet(JobConfig.ItemIdPath);
            }
        }

        if (downloader == null)
        {
            throw new InvalidOperationException("No downloader found.");
        }

        await foreach (var result in downloader.DownloadAsync(jobTaskUri, JobTask.ItemData, JobTask.GetMetadata()))
        {
            itemIds?.Add(result.ItemId);
        }

        if (itemIds != null)
        {
            using var scope = _serviceLocator.BeginLifetimeScope();
            var itemIdSetService = scope.GetRequiredService<IItemIdSetService>();
            itemIdSetService.WriteChanges(JobConfig.ItemIdPath!, itemIds);
        }
    }
}
