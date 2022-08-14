using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;
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
        if (JobTask == null)
        {
            throw new InvalidOperationException("Job task is not set.");
        }

        try
        {

        }
        finally
        {
            using var scope = _serviceLocator.BeginLifetimeScope();
            var jobManager = scope.GetRequiredService<IJobManager>();

            JobTask.Deactivate();
            await jobManager.UpdateJobAsync(Job, CancellationToken.None);
        }
    }
}
