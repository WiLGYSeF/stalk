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
            Debug.WriteLine($"{Job!.Id}: {DateTime.Now} doing work...");
            await Task.Delay(2000);
        }

        if (cancellationToken.HasValue && cancellationToken.Value.IsCancellationRequested)
        {
            return;
        }

        if (!Job!.HasUnfinishedTasks)
        {
            await _jobManager.SetJobDoneAsync(Job);
        }
    }
}
