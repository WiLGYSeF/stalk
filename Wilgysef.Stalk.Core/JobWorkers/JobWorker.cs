using Wilgysef.Stalk.Core.Models.Jobs;

namespace Wilgysef.Stalk.Core.JobWorkers;

public class JobWorker : IJobWorker
{
    public Job? Job { get; private set; }

    public JobWorker() { }

    public JobWorker WithJob(Job job)
    {
        Job = job;
        return this;
    }

    public async Task Work()
    {
        await Task.Delay(2000);
    }
}
