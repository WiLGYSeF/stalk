﻿using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Wilgysef.Stalk.Core.JobTaskWorkerServices;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.ObjectInstances;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Core.JobWorkers;

public class JobWorker : IJobWorker
{
    public Job Job
    {
        get => _job;
        private set
        {
            _job = value;
            _jobConfig = _job.GetConfig();

            WorkerLimit = _jobConfig?.MaxTaskWorkerCount > 0
                ? _jobConfig?.MaxTaskWorkerCount ?? 4
                : 4;
        }
    }
    private Job _job = null!;
    private JobConfig _jobConfig = null!;

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
    private int _workerLimit = 4;

    public TimeSpan TaskWaitTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public TimeSpan NoTaskTimeout { get; set; } = TimeSpan.FromMinutes(1);


    private TimeSpan NoTaskDelay { get; set; } = TimeSpan.FromSeconds(15);

    private readonly Dictionary<Task, long> _tasks = new();

    private DateTime? _lastTimeWithNoTasks;

    private readonly IServiceLifetimeScope _lifetimeScope;
    private readonly ILogger? _logger;
    private readonly IObjectInstanceHandle<ILogger>? _loggerHandle;

    public JobWorker(
        IServiceLifetimeScope lifetimeScope,
        ILogger? logger,
        IObjectInstanceHandle<ILogger>? loggerHandle,
        Job job)
    {
        _lifetimeScope = lifetimeScope;
        _logger = logger;
        _loggerHandle = loggerHandle;
        Job = job;
    }

    public virtual async Task WorkAsync(CancellationToken cancellationToken = default)
    {
        var isDone = false;

        using var _ = _logger?.BeginScope("Job {JobId}", Job.Id);

        try
        {
            _logger?.LogInformation("Job {JobId} starting.", Job.Id);

            if (!Job.IsActive)
            {
                using var scope = _lifetimeScope.BeginLifetimeScope();
                var jobManager = scope.GetRequiredService<IJobManager>();
                await jobManager.SetJobActiveAsync(Job, cancellationToken);
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
                    _logger?.LogDebug("Job {JobId} wait for task succeeded.", Job.Id);
                    RemoveCompletedTasks(taskArray);
                }
                else
                {
                    _logger?.LogDebug("Job {JobId} wait for task timed out.", Job.Id);
                }
            }

            await ReloadJobAsync();
            if (!Job.HasUnfinishedTasks || !JobTaskFailuresLessThanMaxFailures())
            {
                isDone = true;
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogInformation("Job {JobId} worker cancelled.", Job?.Id);
            throw;
        }
        catch (Exception exception)
        {
            _logger?.LogError(exception, "Job {JobId} threw unexpected exception.", Job?.Id);
        }
        finally
        {
            _logger?.LogInformation("Job {JobId} stopping.", Job?.Id);

            if (Job != null)
            {
                await RetryActionAsync(
                    async () =>
                    {
                        using var scope = _lifetimeScope.BeginLifetimeScope();
                        var jobManager = scope.GetRequiredService<IJobManager>();
                        await ReloadJobAsync(jobManager);

                        if (isDone)
                        {
                            Job.Done();
                        }

                        Job.Deactivate();
                        await jobManager.UpdateJobAsync(Job, false, CancellationToken.None);
                    },
                    exception => _logger?.LogError(exception, "Failed to update Job {JobId} state.", Job?.Id),
                    TimeSpan.FromSeconds(1));
            }
        }
    }

    public void Dispose()
    {
        _loggerHandle?.Dispose();

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
            _logger?.LogInformation("Job {JobId} exceeded maximum failure count: {MaxFailures}.", Job.Id, _jobConfig.MaxFailures.GetValueOrDefault());
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
            var task = await jobTaskWorkerService.StartJobTaskWorkerAsync(jobTask, cancellationToken);
            if (task != null)
            {
                _tasks.Add(task, jobTask.Id);
                _logger?.LogDebug("Job {JobId} added task worker for {JobTaskId}.", Job.Id, jobTask.Id);
            }
            else
            {
                _logger?.LogDebug("Job {JobId} task worker already started for {JobTaskId}.", Job.Id, jobTask.Id);
            }
        }
    }

    private void RemoveCompletedTasks(Task[] tasks)
    {
        foreach (var task in tasks)
        {
            if (task.IsCompleted)
            {
                _tasks.Remove(task, out var jobTaskId);
                _logger?.LogDebug("Job {JobId} removed completed task for {JobTaskId}.", Job!.Id, jobTaskId);
            }
        }
    }

    /// <summary>
    /// Checks if the time period of no tasks exceeds the <see cref="NoTaskTimeout"/> threshold.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if there are no tasks and the timeout exceeded or if all job tasks are done or if maximum job task failures have been reached,
    /// <see langword="false"/> if there are no tasks and the timeout did not exceed,
    /// or <see langword="null"/> if there are running tasks.</returns>
    private bool? CheckTimeout()
    {
        if (_tasks.Count != 0)
        {
            return null;
        }
        if (Job.Tasks.All(t => t.IsDone) || !JobTaskFailuresLessThanMaxFailures())
        {
            return true;
        }

        if (!_lastTimeWithNoTasks.HasValue)
        {
            _lastTimeWithNoTasks = DateTime.Now;
        }

        return (DateTime.Now - _lastTimeWithNoTasks.Value) >= NoTaskTimeout;
    }

    private static async Task<bool> RetryActionAsync(Func<Task> action, Action<Exception> failAction, TimeSpan timeSpan)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;

        while (!success && stopwatch.Elapsed < timeSpan)
        {
            try
            {
                await action();
                success = true;
            }
            catch (Exception exception)
            {
                failAction(exception);
            }
        }

        return success;
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
        return await ReloadJobAsync(jobManager);
    }

    private async Task<Job> ReloadJobAsync(IJobManager jobManager)
    {
        var job = await jobManager.GetJobAsync(Job!.Id);

        Job = job;
        return job;
    }
}
