using Wilgysef.Stalk.Core.JobTaskWorkerServices;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Core.JobWorkers;

public class JobWorker : IJobWorker
{
    public Job? Job { get; private set; } = null!;

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

    public int TaskWaitTimeoutMilliseconds { get; set; } = 10 * 1000;

    private readonly List<Task> _tasks = new();

    private readonly IServiceLifetimeScope _lifetimeScope;

    private bool _working = false;

    public JobWorker(IServiceLifetimeScope lifetimeScope)
    {
        _lifetimeScope = lifetimeScope;
    }

    public IJobWorker WithJob(Job job)
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
        if (Job == null)
        {
            throw new InvalidOperationException("Job is not set.");
        }

        _working = true;

        if (!Job.IsActive)
        {
            using var scope = _lifetimeScope.BeginLifetimeScope();
            var jobManager = scope.GetRequiredService<IJobManager>();
            await jobManager.SetJobActiveAsync(Job, cancellationToken);
        }

        try
        {
            while (Job.HasUnfinishedTasks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await CreateJobTaskWorkers(cancellationToken);
                if (_tasks.Count == 0)
                {
                    continue;
                }

                cancellationToken.ThrowIfCancellationRequested();

                var taskArray = _tasks.ToArray();
                var taskCompletedIndex = Task.WaitAny(taskArray, TaskWaitTimeoutMilliseconds, cancellationToken);

                if (taskCompletedIndex != -1)
                {
                    if (taskArray.Any(t => t.Exception != null))
                    {
                        // TODO: do something?
                    }

                    RemoveCompletedTasks(taskArray);
                }
            }

            // TODO: handle job with all paused tasks

            await ReloadJobAsync();
            Job.Done();
        }
        catch (OperationCanceledException) { }
        catch (Exception exc)
        {
            throw;
        }
        finally
        {
            using var scope = _lifetimeScope.BeginLifetimeScope();
            var jobManager = scope.GetRequiredService<IJobManager>();

            Job.Deactivate();
            await jobManager.UpdateJobAsync(Job, CancellationToken.None);
        }
    }

    public void Dispose()
    {
        _lifetimeScope.Dispose();

        GC.SuppressFinalize(this);
    }

    private async Task CreateJobTaskWorkers(CancellationToken cancellationToken)
    {
        if (_tasks.Count >= WorkerLimit)
        {
            return;
        }

        await ReloadJobAsync();

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
            _tasks.Add(await jobTaskWorkerService.StartJobTaskWorkerAsync(jobTask, cancellationToken));
        }
    }

    private void RemoveCompletedTasks(Task[] tasks)
    {
        foreach (var task in tasks)
        {
            if (task.IsCompleted)
            {
                _tasks.Remove(task);
            }
        }
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
