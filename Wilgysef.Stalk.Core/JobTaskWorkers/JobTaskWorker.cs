﻿using Microsoft.Extensions.Logging;
using System.Net;
using System.Security.Cryptography;
using Wilgysef.Stalk.Core.DownloadSelectors;
using Wilgysef.Stalk.Core.ExtractorCacheObjectCollectionServices;
using Wilgysef.Stalk.Core.ExtractorHttpClientFactories;
using Wilgysef.Stalk.Core.ItemIdSetServices;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Exceptions;
using Wilgysef.Stalk.Core.Shared.Extractors;
using Wilgysef.Stalk.Core.Shared.IdGenerators;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Core.JobTaskWorkers;

public class JobTaskWorker : IJobTaskWorker
{
    private const int JobTaskPriorityRetryChange = 100;

    public JobTask JobTask { get; protected set; }

    public ILogger? Logger { get; set; }

    private JobConfig JobConfig { get; set; }

    private readonly IServiceLifetimeScope _lifetimeScope;

    public JobTaskWorker(
        IServiceLifetimeScope lifetimeScope,
        JobTask jobTask)
    {
        _lifetimeScope = lifetimeScope;
        JobTask = jobTask;
        JobConfig = JobTask.Job.GetConfig();
    }

    public virtual async Task WorkAsync(CancellationToken cancellationToken = default)
    {
        var isDone = false;
        JobTaskFailResult? fail = null;

        using var _ = Logger?.BeginScope("Job task {JobTaskId}", JobTask.Id);

        try
        {
            Logger?.LogInformation("Job task {JobTaskId} starting.", JobTask.Id);

            using (var scope = _lifetimeScope.BeginLifetimeScope())
            {
                var jobTaskManager = scope.GetRequiredService<IJobTaskManager>();
                JobTask = await jobTaskManager.GetJobTaskAsync(JobTask.Id, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                if (!JobTask.IsActive)
                {
                    await jobTaskManager.SetJobTaskActiveAsync(JobTask, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            JobConfig = JobTask.Job.GetConfig();

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

            isDone = true;
        }
        catch (OperationCanceledException)
        {
            Logger?.LogInformation("Job task {JobTaskId} worker cancelled.", JobTask?.Id);
            throw;
        }
        catch (Exception exception)
        {
            Logger?.LogError(exception, "Job task {JobTaskId} failed.", JobTask?.Id);

            if (JobTask != null)
            {
                var workerException = exception as JobTaskWorkerException;

                var retryJobTask = await CheckAndRetryJobAsync(exception);

                fail = new(
                    retryJobTask?.Id,
                    workerException?.Code,
                    exception.Message,
                    exception.ToString());
            }
        }
        finally
        {
            Logger?.LogInformation("Job task {JobTaskId} stopping.", JobTask?.Id);

            if (JobTask != null)
            {
                try
                {
                    using var scope = _lifetimeScope.BeginLifetimeScope();
                    var jobTaskManager = scope.GetRequiredService<IJobTaskManager>();
                    JobTask = await jobTaskManager.GetJobTaskAsync(JobTask.Id, CancellationToken.None);

                    if (fail.HasValue)
                    {
                        JobTask.Fail(
                            fail.Value.RetryJobTaskId,
                            fail.Value.Code,
                            fail.Value.Message,
                            fail.Value.Detail);
                    }
                    else if (isDone)
                    {
                        JobTask.Success();
                    }

                    JobTask.Deactivate();
                    await jobTaskManager.UpdateJobTaskAsync(JobTask, CancellationToken.None);
                }
                catch (Exception exception)
                {
                    Logger?.LogError(exception, "Failed to update Job task {JobTaskId} state.", JobTask?.Id);
                }
            }
        }
    }

    protected virtual async Task ExtractAsync(CancellationToken cancellationToken)
    {
        using var scope = _lifetimeScope.BeginLifetimeScope();

        var jobTaskUri = new Uri(JobTask!.Uri);
        var extractor = scope.GetRequiredService<IEnumerable<IExtractor>>()
            .FirstOrDefault(e => e.CanExtract(jobTaskUri));

        if (extractor == null)
        {
            throw new JobTaskWorkerException(
                StalkErrorCodes.JobTaskWorkerNoExtractor,
                $"No extractor found to extract from {jobTaskUri}");
        }

        var extractorConfig = JobConfig.GetExtractorConfig(extractor);

        var extractorHttpClientService = _lifetimeScope.GetRequiredService<IExtractorHttpClientService>();
        extractor.SetHttpClient(extractorHttpClientService.GetHttpClient(extractor, extractorConfig));

        var extractorCacheObjectCollectionService = _lifetimeScope.GetRequiredService<IExtractorCacheObjectCollectionService>();
        extractor.Cache = extractorCacheObjectCollectionService.GetCache(extractor);
        extractor.Config = extractorConfig;

        var idGenerator = scope.GetRequiredService<IIdGenerator<long>>();
        var itemIdSetService = scope.GetRequiredService<IItemIdSetService>();
        var newTasks = new List<JobTask>();
        IItemIdSet? itemIds = null;

        Logger?.LogInformation("Job task {JobTaskId} extracting with {Extractor} from {Uri}", JobTask.Id, extractor.Name, GetUriForLog(jobTaskUri));

        if (JobConfig.SaveItemIds && JobConfig.ItemIdPath != null)
        {
            itemIds = await itemIdSetService.GetItemIdSetAsync(JobConfig.ItemIdPath, JobTask.JobId);
            var itemId = JobTask.ItemId ?? extractor.GetItemId(jobTaskUri);
            if (itemId != null && itemIds.Contains(itemId))
            {
                Logger?.LogInformation("Job task {JobTaskId} skipping item {ItemId} from {Uri}", JobTask.Id, itemId, GetUriForLog(jobTaskUri));
                return;
            }
        }

        var resultsCount = 0;
        var resultsSkipped = 0;

        await foreach (var result in extractor.ExtractAsync(jobTaskUri, JobTask.ItemData, JobTask.GetMetadata(), cancellationToken))
        {
            resultsCount++;

            if (itemIds != null && result.ItemId != null && itemIds.Contains(result.ItemId))
            {
                Logger?.LogInformation("Job task {JobTaskId} skipping item {ItemId} from {Uri}", JobTask.Id, result.ItemId, GetUriForLog(jobTaskUri));
                resultsSkipped++;
                continue;
            }

            Logger?.LogInformation("Job task {JobTaskId} extracted {Uri}", JobTask.Id, GetUriForLog(result.Uri));
            Logger?.LogDebug("Job task {JobTaskId} extracted {Uri}: {@Result}", JobTask.Id, GetUriForLog(result.Uri), new
            {
                result.ItemId,
                result.Type,
                result.Priority,
                result.Name,
                result.ItemData,
                result.DownloadRequestData,
            });

            var jobTaskBuilder = new JobTaskBuilder()
                .WithExtractResult(JobTask, result)
                .WithId(idGenerator.CreateId());

            if (result.DownloadRequestData != null)
            {
                jobTaskBuilder.WithDownloadRequestData(result.DownloadRequestData);
            }

            if (JobConfig.Delay.TaskDelay != null)
            {
                jobTaskBuilder.WithDelayTime(TimeSpan.FromSeconds(RandomInt(
                    JobConfig.Delay.TaskDelay.Min,
                    JobConfig.Delay.TaskDelay.Max)));
            }

            var newTask = jobTaskBuilder.Create();

            Logger?.LogInformation("Job task {JobTaskId} adding new job task {NewJobTaskId}", JobTask.Id, newTask.Id);
            newTasks.Add(newTask);
        }

        if (!JobConfig.StopWithNoNewItemIds || newTasks.Any(t => t.ItemId != null))
        {
            var jobTaskManager = scope.GetRequiredService<IJobTaskManager>();
            await jobTaskManager.CreateJobTasksAsync(newTasks, CancellationToken.None);
        }
        else if (resultsSkipped > 0)
        {
            Logger?.LogInformation("Job task {JobTaskId} skipped {ResultsSkipped} since they had no item Ids,", JobTask.Id, resultsSkipped);
        }
    }

    protected virtual async Task DownloadAsync(CancellationToken cancellationToken)
    {
        using var scope = _lifetimeScope.BeginLifetimeScope();
        var downloaderSelector = scope.GetRequiredService<IDownloadSelector>();
        var itemIdSetService = scope.GetRequiredService<IItemIdSetService>();

        var jobTaskUri = new Uri(JobTask!.Uri);
        var downloader = downloaderSelector.SelectDownloader(jobTaskUri);

        if (downloader == null)
        {
            throw new InvalidOperationException("No downloader found.");
        }
        if (!JobConfig!.DownloadData)
        {
            Logger?.LogInformation("Job task {JobTaskId} skipping download", JobTask.Id);
            return;
        }
        if (JobConfig.DownloadFilenameTemplate == null)
        {
            throw new JobTaskWorkerException("No download filename template given.");
        }

        // TODO: HttpClient, user agent
        //downloader.SetHttpClient(_httpClient);
        downloader.Config = JobConfig.GetDownloaderConfig(downloader);

        IItemIdSet? itemIds = null;
        if (JobConfig.SaveItemIds && JobConfig.ItemIdPath != null)
        {
            itemIds = await itemIdSetService.GetItemIdSetAsync(JobConfig.ItemIdPath, JobTask.JobId);
            if (JobTask.ItemId != null && itemIds.Contains(JobTask.ItemId))
            {
                Logger?.LogInformation("Job task {JobTaskId} skipping item {ItemId} from {Uri}", JobTask.Id, JobTask.ItemId, GetUriForLog(jobTaskUri));
                return;
            }
        }

        Logger?.LogInformation("Job task {JobTaskId} downloading with {Downloader} from {Uri}", JobTask.Id, downloader.Name, GetUriForLog(jobTaskUri));

        await foreach (var result in downloader.DownloadAsync(
            jobTaskUri,
            JobConfig.DownloadFilenameTemplate,
            JobTask.ItemId,
            JobConfig.SaveMetadata ? JobConfig.MetadataFilenameTemplate : null,
            JobTask.GetMetadata(),
            requestData: JobTask.DownloadRequestData,
            cancellationToken: cancellationToken))
        {
            Logger?.LogInformation("Job task {JobTaskId} downloaded {Uri} to {Path}", JobTask.Id, GetUriForLog(result.Uri), result.Path);
            Logger?.LogDebug("Job task {JobTaskId} downloaded {Uri}: {@Result}", JobTask.Id, GetUriForLog(result.Uri), new
            {
                result.ItemId,
                result.MetadataPath,
            });

            if (result.ItemId != null)
            {
                itemIds?.Add(result.ItemId);
            }
        }

        if (itemIds != null)
        {
            await itemIdSetService.WriteChangesAsync(JobConfig.ItemIdPath!, itemIds);
        }
    }

    protected virtual async Task<JobTask?> CheckAndRetryJobAsync(Exception exception)
    {
        if (!ShouldRetryJobTask(JobTask, exception, out var tooManyRequests))
        {
            return null;
        }

        try
        {
            using var scope = _lifetimeScope.BeginLifetimeScope();
            var idGenerator = scope.GetRequiredService<IIdGenerator<long>>();
            var jobTaskManager = scope.GetRequiredService<IJobTaskManager>();

            var retryTask = CreateRetryJobTask(idGenerator.CreateId(), tooManyRequests);
            Logger?.LogInformation("Job task {JobTaskId} creating retry task {RetryJobTaskId}.", JobTask.Id, retryTask.Id);

            await jobTaskManager.CreateJobTaskAsync(retryTask, CancellationToken.None);
            return retryTask;
        }
        catch (Exception retryTaskException)
        {
            Logger?.LogError(retryTaskException, "Job task {JobTaskId} failed to create a retry job task.", JobTask.Id);
            return null;
        }
    }

    protected virtual JobTask CreateRetryJobTask(long jobTaskId, bool tooManyRequests)
    {
        var jobTaskBuilder = new JobTaskBuilder()
            .WithRetryJobTask(JobTask!)
            .WithId(jobTaskId)
            .WithPriority(JobTask!.Priority - JobTaskPriorityRetryChange);

        if (tooManyRequests && JobConfig!.Delay.TooManyRequestsDelay != null)
        {
            jobTaskBuilder.WithDelayTime(TimeSpan.FromSeconds(RandomInt(
                JobConfig.Delay.TooManyRequestsDelay.Min,
                JobConfig.Delay.TooManyRequestsDelay.Max)));
        }
        else if (JobConfig.Delay?.TaskFailedDelay != null)
        {
            jobTaskBuilder.WithDelayTime(TimeSpan.FromSeconds(RandomInt(
                JobConfig.Delay.TaskFailedDelay.Min,
                JobConfig.Delay.TaskFailedDelay.Max)));
        }

        return jobTaskBuilder.Create();
    }

    protected virtual bool ShouldRetryJobTask(JobTask jobTask, Exception exception, out bool tooManyRequests)
    {
        bool retry = false;
        tooManyRequests = false;

        switch (exception)
        {
            case HttpRequestException httpException:
                if (httpException.StatusCode.HasValue && IsStatusCodeRetry(httpException.StatusCode.Value))
                {
                    retry = true;
                    tooManyRequests = httpException.StatusCode.Value == HttpStatusCode.TooManyRequests;
                }
                break;
            case JobTaskWorkerException workerException:
                break;
            default:
                retry = true;
                break;
        }

        return retry;
    }

    protected virtual bool IsStatusCodeRetry(HttpStatusCode statusCode)
    {
        return statusCode == HttpStatusCode.RequestTimeout
            || statusCode == HttpStatusCode.TooManyRequests
            || ((int)statusCode >= 500 && (int)statusCode < 600
                && statusCode != HttpStatusCode.HttpVersionNotSupported);
    }

    protected virtual int RandomInt(int min, int max)
    {
        return min != max && min < max
            ? RandomNumberGenerator.GetInt32(min, max)
            : min;
    }

    private static string GetUriForLog(Uri uri)
    {
        return uri.Scheme == "data"
            ? "data:..."
            : uri.ToString();
    }

    private struct JobTaskFailResult
    {
        public long? RetryJobTaskId { get; }

        public string? Code { get; }

        public string Message { get; }

        public string Detail { get; }

        public JobTaskFailResult(
            long? retryJobTaskId,
            string? code,
            string message,
            string detail)
        {
            RetryJobTaskId = retryJobTaskId;
            Code = code;
            Message = message;
            Detail = detail;
        }
    }
}
