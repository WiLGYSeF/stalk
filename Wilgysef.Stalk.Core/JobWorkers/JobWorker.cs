using System.Diagnostics;
using Wilgysef.Stalk.Core.Models.Jobs;

namespace Wilgysef.Stalk.Core.JobWorkers;

public class JobWorker : IJobWorker
{
    public Job? Job { get; private set; }

    private readonly IJobManager _jobManager;

    public JobWorker(
        IJobManager jobManager)
    {
        _jobManager = jobManager;
    }

    public JobWorker WithJob(Job job)
    {
        Job = job;
        return this;
    }

    public async Task WorkAsync(CancellationToken? cancellationToken = null)
    {
        await _jobManager.SetJobActiveAsync(Job!);

        while (!cancellationToken.HasValue || !cancellationToken.Value.IsCancellationRequested)
        {
            Debug.WriteLine($"{Job!.Id}: doing work...");
            await Task.Delay(1000);
        }

        if (!Job!.HasUnfinishedTasks && (!cancellationToken.HasValue || !cancellationToken.Value.IsCancellationRequested))
        {
            await _jobManager.SetJobDoneAsync(Job);
        }
    }
}
