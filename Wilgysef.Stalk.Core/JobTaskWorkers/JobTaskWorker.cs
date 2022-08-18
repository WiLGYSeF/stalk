using IdGen;
using Wilgysef.Stalk.Core.Downloaders;
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
    public Job? Job { get; private set; }

    public JobTask? JobTask { get; private set; }

    private readonly IServiceLocator _serviceLocator;

    public JobTaskWorker(
        IServiceLocator serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    public JobTaskWorker WithJobTask(Job job, JobTask jobTask)
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
            switch (JobTask.Type)
            {
                case JobTaskType.Extract:
                    await ExtractAsync();
                    break;
                case JobTaskType.Download:
                    await DownloadAsync();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        finally
        {
            using var scope = _serviceLocator.BeginLifetimeScope();
            var jobManager = scope.GetRequiredService<IJobManager>();

            JobTask.Deactivate();
            await jobManager.UpdateJobAsync(Job, CancellationToken.None);
        }
    }

    private async Task ExtractAsync()
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

        await foreach (var result in extractor.ExtractAsync(jobTaskUri, JobTask.ItemData, JobTask.GetMetadata()))
        {
            using var scope = _serviceLocator.BeginLifetimeScope();
            var idGenerator = scope.GetRequiredService<IIdGenerator<long>>();

            Job!.AddTask(new JobTaskBuilder()
                .WithId(idGenerator.CreateId())
                .WithExtractResult(JobTask, result)
                .Create());
        }
    }

    private async Task DownloadAsync()
    {
        var jobTaskUri = new Uri(JobTask!.Uri);

        IDownloader? downloader = null;
        using (var scope = _serviceLocator.BeginLifetimeScope())
        {
            var defaultDownloader = scope.GetRequiredService<IDefaultDownloader>();
            downloader = scope.GetRequiredService<IEnumerable<IDownloader>>()
                .FirstOrDefault(d => d is not IDefaultDownloader && d.CanDownload(jobTaskUri))
                ?? defaultDownloader;
        }

        if (downloader == null)
        {
            throw new InvalidOperationException("No downloader found.");
        }

        await foreach (var result in downloader.DownloadAsync(jobTaskUri, JobTask.ItemData, JobTask.GetMetadata()))
        {

        }
    }
}
