using System.Diagnostics;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Core.JobWorkers;

public class JobWorker : IJobWorker
{
    public Job? Job { get; private set; }

    private readonly IServiceLocator _serviceLocator;

    public JobWorker(
        IServiceLocator serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    public JobWorker WithJob(Job job)
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
            await jobManager.SetJobActiveAsync(Job!, cancellationToken);
        }

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Debug.WriteLine($"{Job!.Id}: {DateTime.Now} doing work...");
                await Task.Delay(2000, cancellationToken);
            }
        } catch (OperationCanceledException) { }

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (!Job.HasUnfinishedTasks)
        {
            using var scope = _serviceLocator.BeginLifetimeScope();
            var jobManager = scope.GetRequiredService<IJobManager>();
            await jobManager.SetJobDoneAsync(Job);
        }
    }
}
