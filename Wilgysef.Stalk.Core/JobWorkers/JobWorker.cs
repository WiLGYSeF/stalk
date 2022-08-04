using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Enums;

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

    public async Task Work(CancellationToken? cancellationToken = null)
    {
        await _jobManager.SetJobActiveAsync(Job);

        await Task.Delay(2000);

        if (!Job.HasUnfinishedTasks)
        {
            await _jobManager.SetJobDoneAsync(Job);
        }
    }
}
