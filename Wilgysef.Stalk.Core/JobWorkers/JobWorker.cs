using Wilgysef.Stalk.Core.JobTaskWorkerServices;
using Wilgysef.Stalk.Core.JobWorkerServices;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Core.JobWorkers;

public class JobWorker : IJobWorker
{
    public Job? Job { get; private set; } = null!;

    public int WorkerLimit { get; set; } = 4;

    public int TaskWaitTimeoutMilliseconds { get; set; } = 1000;

    private readonly List<Task> _tasks;

    private readonly IServiceLocator _serviceLocator;

    public JobWorker(
        IServiceLocator serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    public IJobWorker WithJob(Job job)
    {
        Job = job;
        return this;
    }

    public async Task WorkAsync(CancellationToken cancellationToken = default)
    {
        if (Job == null)
        {
            throw new InvalidOperationException("Job is not set.");
        }

        if (!Job.IsActive)
        {
            using var scope = _serviceLocator.BeginLifetimeScope();
            var jobManager = scope.GetRequiredService<IJobManager>();
            await jobManager.SetJobActiveAsync(Job, cancellationToken);
        }

        try
        {
            while (!cancellationToken.IsCancellationRequested && Job.HasUnfinishedTasks)
            {
                await CreateJobTaskWorkers(cancellationToken);

                var taskArray = _tasks.ToArray();
                var taskCompletedIndex = Task.WaitAny(taskArray, TaskWaitTimeoutMilliseconds, cancellationToken);

                if (taskCompletedIndex != -1)
                {
                    RemoveCompletedTasks(taskArray);
                }
            }
        }
        finally
        {
            using var scope = _serviceLocator.BeginLifetimeScope();
            var jobManager = scope.GetRequiredService<IJobManager>();

            if (!Job.HasUnfinishedTasks)
            {
                await jobManager.SetJobDoneAsync(Job, CancellationToken.None);
            }
            else
            {
                Job.ChangeState(JobState.Inactive);
                await jobManager.UpdateJobAsync(Job, CancellationToken.None);
            }

            var jobWorkerCollectionService = scope.GetRequiredService<IJobWorkerCollectionService>();
            jobWorkerCollectionService.RemoveJobWorker(this);
        }
    }

    private async Task CreateJobTaskWorkers(CancellationToken cancellationToken)
    {
        if (_tasks.Count >= WorkerLimit)
        {
            return;
        }

        using var scope = _serviceLocator.BeginLifetimeScope();
        var jobTaskWorkerService = scope.GetRequiredService<IJobTaskWorkerService>();

        var jobTasks = Job!.GetQueuedTasksByPriority();
        var jobTaskIndex = 0;

        while (_tasks.Count < WorkerLimit && jobTaskIndex < jobTasks.Count)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var jobTask = jobTasks[jobTaskIndex];
            _tasks.Add(await jobTaskWorkerService.StartJobTaskWorkerAsync(Job, jobTask, cancellationToken));
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
}
