using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;

namespace Wilgysef.Stalk.Core.JobWorkerFactories;

public class JobWorkerFactory : IJobWorkerFactory
{
    private readonly IJobManager _jobManager;

    public JobWorkerFactory(
        IJobManager jobManager)
    {
        _jobManager = jobManager;
    }

    public JobWorker CreateWorker(Job job)
    {
        var jobWorker = new JobWorker(_jobManager);
        jobWorker.WithJob(job);
        return jobWorker;
    }
}
