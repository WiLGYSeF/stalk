using Microsoft.Extensions.Logging;
using Wilgysef.Stalk.Core.JobHttpClientCollectionServices;
using Wilgysef.Stalk.Core.JobTaskWorkerServices;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;
using Wilgysef.Stalk.Core.UserAgentGenerators;

namespace Wilgysef.Stalk.Core.JobWorkers;

public class JobWorker : IJobWorker
{
    private Job? _job;

    public Job? Job
    {
        get => _job;
        private set
        {
            _job = value;
            _jobConfig = _job?.GetConfig();

            WorkerLimit = _jobConfig?.MaxTaskWorkerCount > 0
                ? _jobConfig?.MaxTaskWorkerCount ?? 4
                : 4;
        }
    }

    private JobConfig? _jobConfig = null;

    private int _workerLimit = 4;

    public int WorkerLimit
    {
        get => _workerLimit;
        set
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Worker limit must be at least 1.");
            }
            _workerLimit = value;
        }
    }

    public TimeSpan TaskWaitTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public TimeSpan NoTaskTimeout { get; set; } = TimeSpan.FromMinutes(1);

    public ILogger? Logger { get; set; }

    private TimeSpan NoTaskDelay { get; set; } = TimeSpan.FromSeconds(15);

    private readonly Dictionary<Task, long> _tasks = new();

    private bool _working = false;

    private DateTime? _lastTimeWithNoTasks;

    private readonly IServiceLifetimeScope _lifetimeScope;
    private HttpClient _httpClient;

    public JobWorker(
        IServiceLifetimeScope lifetimeScope,
        HttpClient httpClient)
    {
        _lifetimeScope = lifetimeScope;
        _httpClient = httpClient;
    }

    // TODO: use constructor
    public virtual IJobWorker WithJob(Job job)
    {
        if (_working)
        {
            throw new InvalidOperationException("Cannot set job when worker is already working");
        }

        Job = job;
        return this;
    }

    public virtual async Task WorkAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (Job == null)
            {
                throw new InvalidOperationException("Job is not set.");
            }

            _working = true;

            Logger?.LogInformation("Job {JobId} starting.", Job.Id);

            using (var scope = _lifetimeScope.BeginLifetimeScope())
            {
                if (!Job.IsActive)
                {
                    var jobManager = scope.GetRequiredService<IJobManager>();
                    await jobManager.SetJobActiveAsync(Job, cancellationToken);
                }

                var jobHttpClientCollectionService = scope.GetRequiredService<IJobHttpClientCollectionService>();
                if (!jobHttpClientCollectionService.TryGetHttpClient(Job.Id, out var client))
                {
                    jobHttpClientCollectionService.SetHttpClient(Job.Id, _httpClient);

                    var userAgentGenerator = scope.GetService<IUserAgentGenerator>();
                    if (userAgentGenerator != null)
                    {
                        _httpClient.DefaultRequestHeaders.Add("User-Agent", userAgentGenerator.Generate());
                    }
                }
                else
                {
                    _httpClient.Dispose();
                    _httpClient = client;
                }
            }

            while (Job.HasUnfinishedTasks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await CreateJobTaskWorkers(cancellationToken);

                var timeoutCheck = CheckTimeout();
                if (timeoutCheck.HasValue)
                {
                    if (timeoutCheck.Value)
                    {
                        break;
                    }

                    await Task.Delay(NoTaskDelay, cancellationToken);
                    continue;
                }

                cancellationToken.ThrowIfCancellationRequested();

                var taskArray = _tasks.Keys.ToArray();
                var taskCompletedIndex = Task.WaitAny(taskArray, (int)TaskWaitTimeout.TotalMilliseconds, cancellationToken);

                if (taskCompletedIndex != -1)
                {
                    Logger?.LogDebug("Job {JobId} wait for task succeeded.", Job.Id);
                    RemoveCompletedTasks(taskArray);
                }
                else
                {
                    Logger?.LogDebug("Job {JobId} wait for task timed out.", Job.Id);
                }
            }

            await ReloadJobAsync();
            if (!Job.HasUnfinishedTasks || !JobTaskFailuresLessThanMaxFailures())
            {
                Job.Done();
            }
        }
        catch (OperationCanceledException)
        {
            Logger?.LogInformation("Job {JobId} worker cancelled.", Job?.Id);
            throw;
        }
        catch (Exception exception)
        {
            Logger?.LogError(exception, "Job {JobId} threw unexpected exception.", Job?.Id);
        }
        finally
        {
            Logger?.LogInformation("Job {JobId} stopping.", Job?.Id);

            if (Job != null)
            {
                using var scope = _lifetimeScope.BeginLifetimeScope();
                var jobManager = scope.GetRequiredService<IJobManager>();

                Job.Deactivate();
                await jobManager.UpdateJobAsync(Job, CancellationToken.None);
            }
        }
    }

    public virtual void Dispose()
    {
        _lifetimeScope.Dispose();
        _httpClient.Dispose();

        GC.SuppressFinalize(this);
    }

    private async Task CreateJobTaskWorkers(CancellationToken cancellationToken)
    {
        if (_tasks.Count >= WorkerLimit)
        {
            return;
        }

        await ReloadJobAsync();
        if (!JobTaskFailuresLessThanMaxFailures())
        {
            return;
        }

        var jobTasks = Job!.GetQueuedTasksByPriority();
        if (jobTasks.Count == 0)
        {
            return;
        }

        using var scope = _lifetimeScope.BeginLifetimeScope();
        var jobTaskWorkerService = scope.GetRequiredService<IJobTaskWorkerService>();

        var jobTaskIndex = 0;

        while (_tasks.Count < WorkerLimit && jobTaskIndex < jobTasks.Count)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var jobTask = jobTasks[jobTaskIndex++];
            _tasks.Add(await jobTaskWorkerService.StartJobTaskWorkerAsync(jobTask, cancellationToken), jobTask.Id);
            Logger?.LogDebug("Job {JobId} added task worker for {JobTaskId}.", Job.Id, jobTask.Id);
        }
    }

    private void RemoveCompletedTasks(Task[] tasks)
    {
        foreach (var task in tasks)
        {
            if (task.IsCompleted)
            {
                _tasks.Remove(task, out var jobTaskId);
                Logger?.LogDebug("Job {JobId} removed completed task for {JobTaskId}.", Job!.Id, jobTaskId);
            }
        }
    }

    /// <summary>
    /// Checks if the time period of no tasks exceeds the <see cref="NoTaskTimeout"/> threshold.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if there are no tasks and the timeout exceeded,
    /// <see langword="false"/> if there are no tasks and the timeout did not exceed,
    /// or <see langword="null"/> if there are running tasks.</returns>
    private bool? CheckTimeout()
    {
        if (_tasks.Count != 0)
        {
            return null;
        }

        if (!_lastTimeWithNoTasks.HasValue)
        {
            _lastTimeWithNoTasks = DateTime.Now;
        }

        return (DateTime.Now - _lastTimeWithNoTasks.Value) >= NoTaskTimeout;
    }

    private bool JobTaskFailuresLessThanMaxFailures()
    {
        return !_jobConfig!.MaxFailures.HasValue
            || Job!.Tasks.Count(t => t.State == JobTaskState.Failed) <= _jobConfig.MaxFailures.Value;
    }

    private async Task<Job> ReloadJobAsync()
    {
        using var scope = _lifetimeScope.BeginLifetimeScope();
        return await ReloadJobAsync(scope);
    }

    private async Task<Job> ReloadJobAsync(IServiceLifetimeScope scope)
    {
        var jobManager = scope.GetRequiredService<IJobManager>();
        var job = await jobManager.GetJobAsync(Job!.Id);

        Job = job;
        return job;
    }
}
