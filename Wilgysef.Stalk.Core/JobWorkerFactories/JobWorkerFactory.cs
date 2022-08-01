using Microsoft.Extensions.DependencyInjection;
using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;

namespace Wilgysef.Stalk.Core.JobWorkerFactories;

public class JobWorkerFactory : IJobWorkerFactory
{
    public JobWorker CreateWorker(Job job)
    {
        var jobWorker = new JobWorker();
        jobWorker.WithJob(job);
        return jobWorker;
    }
}
