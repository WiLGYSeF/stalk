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
        using var scope = _serviceLocator.BeginLifetimeScope();
        var jobManager = scope.GetRequiredService<IJobManager>();
        await jobManager.SetJobActiveAsync(Job!);

        while (!cancellationToken.IsCancellationRequested)
        {
            Debug.WriteLine($"{Job!.Id}: {DateTime.Now} doing work...");
            await Task.Delay(2000);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (!Job!.HasUnfinishedTasks)
        {
            await jobManager.SetJobDoneAsync(Job);
        }
    }
}
