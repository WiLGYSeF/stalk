using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;

namespace Wilgysef.Stalk.Core.JobWorkers;

public class JobTaskWorker : IJobTaskWorker
{
    public JobTask? JobTask { get; private set; }

    private readonly IJobManager _jobManager;

    public JobTaskWorker(
        IJobManager jobManager)
    {
        _jobManager = jobManager;
    }

    public JobTaskWorker WithJobTask(JobTask task)
    {
        JobTask = task;
        return this;
    }

    public async Task WorkAsync(CancellationToken? cancellationToken = null)
    {

    }
}
