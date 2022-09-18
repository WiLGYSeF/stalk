using Microsoft.Extensions.Logging;
using System.Net;
using System.Security.Cryptography;
using Wilgysef.Stalk.Core.DownloadSelectors;
using Wilgysef.Stalk.Core.ItemIdSetServices;
using Wilgysef.Stalk.Core.JobExtractorCacheObjectCollectionServices;
using Wilgysef.Stalk.Core.JobHttpClientCollectionServices;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared;
using Wilgysef.Stalk.Core.Shared.Downloaders;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Extractors;
using Wilgysef.Stalk.Core.Shared.IdGenerators;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Core.JobTaskWorkers;

public class JobTaskWorker : IJobTaskWorker
{
    private const int JobTaskPriorityRetryChange = 100;

    public JobTask? JobTask { get; protected set; }

    public ILogger? Logger { get; set; }

    private JobConfig JobConfig { get; set; }

    private bool _working = false;

    private readonly IServiceLifetimeScope _lifetimeScope;
    private HttpClient _httpClient;

    public JobTaskWorker(
        IServiceLifetimeScope lifetimeScope,
        HttpClient httpClient)
    {
        _lifetimeScope = lifetimeScope;
        _httpClient = httpClient;
    }

    public virtual IJobTaskWorker WithJobTask(JobTask jobTask)
    {
        if (_working)
        {
            throw new InvalidOperationException("Cannot set job when worker is already working");
        }

        JobTask = jobTask;
        return this;
    }

    public virtual async Task WorkAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (JobTask == null)
            {
                throw new InvalidOperationException("Job task is not set.");
            }

            _working = true;

            Logger?.LogInformation("Job task {JobTaskId} starting.", JobTask.Id);

            using (var scope = _lifetimeScope.BeginLifetimeScope())
            {
                var jobTaskManager = scope.GetRequiredService<IJobTaskManager>();
                JobTask = await jobTaskManager.GetJobTaskAsync(JobTask.Id, cancellationToken);

                if (!JobTask.IsActive)
                {
                    await jobTaskManager.SetJobTaskActiveAsync(JobTask, CancellationToken.None);
                }

                var jobHttpClientCollectionService = scope.GetRequiredService<IJobHttpClientCollectionService>();
                if (jobHttpClientCollectionService.TryGetHttpClient(JobTask.JobId, out var client))
                {
                    _httpClient.Dispose();
                    _httpClient = client;
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

            JobTask.Success();
        }
        catch (OperationCanceledException)
        {
            Logger?.LogInformation("Job task {JobTaskId} worker cancelled.", JobTask.Id);
            throw;
        }
        catch (Exception exception)
        {
            Logger?.LogError(exception, "Job task {JobTaskId} failed.", JobTask.Id);

            var workerException = exception as JobTaskWorkerException;

            if (RetryJobTask(JobTask, exception, out var tooManyRequests))
            {
                Logger?.LogInformation("Job task {JobTaskId} creating retry task.", JobTask.Id);

                try
                {
                    using var scope = _lifetimeScope.BeginLifetimeScope();
                    var idGenerator = scope.GetRequiredService<IIdGenerator<long>>();
                    var jobTaskManager = scope.GetRequiredService<IJobTaskManager>();

                    var retryTask = CreateRetryJobTask(idGenerator.CreateId(), tooManyRequests);

                    await jobTaskManager.CreateJobTaskAsync(retryTask, CancellationToken.None);
                }
                catch (Exception retryTaskException)
                {
                    Logger?.LogError(retryTaskException, "Job task {JobTaskId} failed to create a retry job task.", JobTask.Id);
                }
            }

            JobTask.Fail(
                errorCode: workerException?.Code,
                errorMessage: exception.Message,
                errorDetail: workerException?.Details ?? exception.ToString());
        }
        finally
        {
            Logger?.LogInformation("Job task {JobTaskId} stopping.", JobTask.Id);

            using var scope = _lifetimeScope.BeginLifetimeScope();
            var jobTaskManager = scope.GetRequiredService<IJobTaskManager>();

            JobTask.Deactivate();
            await jobTaskManager.UpdateJobTaskAsync(JobTask, CancellationToken.None);
        }
    }

    public virtual void Dispose()
    {
        _lifetimeScope.Dispose();
        _httpClient.Dispose();

        GC.SuppressFinalize(this);
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
                "No extractor found.",
                $"No extractor was able to extract from {jobTaskUri}");
        }

        extractor.SetHttpClient(_httpClient);

        var jobExtractorCacheObjectCollectionService = scope.GetRequiredService<IJobExtractorCacheObjectCollectionService>();
        var cacheCollection = jobExtractorCacheObjectCollectionService.GetCacheCollection(JobTask.JobId);
        extractor.Cache = cacheCollection.GetCache(extractor);
        extractor.Config = GetExtractorConfig(extractor);

        Logger?.LogInformation("Job task {JobTaskId} using extractor {Extractor}.", JobTask.Id, extractor.Name);

        var idGenerator = scope.GetRequiredService<IIdGenerator<long>>();
        var itemIdSetService = scope.GetRequiredService<IItemIdSetService>();
        var newTasks = new List<JobTask>();
        IItemIdSet? itemIds = null;

        Logger?.LogInformation("Job task {JobTaskId} extracting from {Uri}", JobTask.Id, jobTaskUri);

        if (JobConfig.SaveItemIds && JobConfig.ItemIdPath != null)
        {
            itemIds = await itemIdSetService.GetItemIdSetAsync(JobConfig.ItemIdPath);
        }

        await foreach (var result in extractor.ExtractAsync(jobTaskUri, JobTask.ItemData, JobTask.GetMetadata(), cancellationToken))
        {
            if (itemIds != null && result.ItemId != null && itemIds.Contains(result.ItemId))
            {
                Logger?.LogInformation("Job task {JobTaskId} skipping item {ItemId} from {Uri}", JobTask.Id, JobTask.ItemId, jobTaskUri);
                continue;
            }

            Logger?.LogInformation("Job task {JobTaskId} extracted {Uri}", JobTask.Id, result.Uri);

            var jobTaskBuilder = new JobTaskBuilder()
                .WithExtractResult(JobTask, result)
                .WithId(idGenerator.CreateId());

            if (JobConfig.Delay?.TaskDelay != null)
            {
                jobTaskBuilder.WithDelayTime(TimeSpan.FromSeconds(RandomInt(
                    JobConfig.Delay.TaskDelay.Min,
                    JobConfig.Delay.TaskDelay.Max)));
            }

            newTasks.Add(jobTaskBuilder.Create());
        }

        if (!JobConfig.StopWithNoNewItemIds || newTasks.Any(t => t.ItemId != null))
        {
            var jobTaskManager = scope.GetRequiredService<IJobTaskManager>();
            await jobTaskManager.CreateJobTasksAsync(newTasks, CancellationToken.None);
        }
        else if (newTasks.Count > 0)
        {
            Logger?.LogInformation("Job task {JobTaskId} skipped {JobTasksCount} since they had no item Ids,", JobTask.Id, newTasks.Count);
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
        if (!JobConfig.DownloadData)
        {
            return;
        }

        downloader.SetHttpClient(_httpClient);
        downloader.Config = GetDownloaderConfig(downloader);

        Logger?.LogInformation("Job task {JobTaskId} using downloader {Downloader}.", JobTask.Id, downloader.Name);

        IItemIdSet? itemIds = null;
        if (JobConfig.SaveItemIds && JobConfig.ItemIdPath != null)
        {
            itemIds = await itemIdSetService.GetItemIdSetAsync(JobConfig.ItemIdPath);
            if (JobTask.ItemId != null && itemIds.Contains(JobTask.ItemId))
            {
                Logger?.LogInformation("Job task {JobTaskId} skipping item {ItemId} from {Uri}", JobTask.Id, JobTask.ItemId, jobTaskUri);
                return;
            }
        }

        Logger?.LogInformation("Job task {JobTaskId} downloading from {Uri}", JobTask.Id, jobTaskUri);

        await foreach (var result in downloader.DownloadAsync(
            jobTaskUri,
            JobConfig.DownloadFilenameTemplate,
            JobTask.ItemId,
            JobTask.ItemData,
            JobConfig.SaveMetadata ? JobConfig.MetadataFilenameTemplate : null,
            JobTask.GetMetadata(),
            cancellationToken))
        {
            Logger?.LogInformation("Job task {JobTaskId} downloaded {Uri}", JobTask.Id, result.Uri);

            itemIds?.Add(result.ItemId);
        }

        if (itemIds != null)
        {
            await itemIdSetService.WriteChangesAsync(JobConfig.ItemIdPath!, itemIds);
        }
    }

    protected virtual JobTask CreateRetryJobTask(long jobTaskId, bool tooManyRequests)
    {
        var jobTaskBuilder = new JobTaskBuilder()
            .WithRetryJobTask(JobTask)
            .WithId(jobTaskId)
            .WithPriority(JobTask.Priority - JobTaskPriorityRetryChange);

        if (tooManyRequests && JobConfig.Delay?.TooManyRequestsDelay != null)
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

    protected virtual bool RetryJobTask(JobTask jobTask, Exception exception, out bool tooManyRequests)
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
            default:
                break;
        }

        return retry;
    }

    protected virtual IDictionary<string, object?> GetExtractorConfig(IExtractor extractor)
    {
        var config = new Dictionary<string, object?>();
        if (JobConfig.ExtractorConfig == null)
        {
            return config;
        }

        GetConfig(JobConfig.ExtractorConfig.Where(c => c.Name == JobConfig.GlobalConfigGroupName), config);
        GetConfig(JobConfig.ExtractorConfig.Where(c => c.Name == extractor.Name), config);
        return config;
    }

    protected virtual IDictionary<string, object?> GetDownloaderConfig(IDownloader downloader)
    {
        var config = new Dictionary<string, object?>();
        if (JobConfig.DownloaderConfig == null)
        {
            return config;
        }

        GetConfig(JobConfig.DownloaderConfig.Where(c => c.Name == JobConfig.GlobalConfigGroupName), config);
        GetConfig(JobConfig.DownloaderConfig.Where(c => c.Name == downloader.Name), config);
        return config;
    }

    protected virtual void GetConfig(IEnumerable<JobConfig.ConfigGroup> configGroups, IDictionary<string, object?> config)
    {
        foreach (var configGroup in configGroups)
        {
            foreach (var (key, val) in configGroup.Config)
            {
                config[key] = val;
            }
        }
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
}
