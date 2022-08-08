using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Core.JobWorkerFactories;

public class JobWorkerFactory : IJobWorkerFactory
{
    private readonly IServiceLocator _serviceLocator;

    public JobWorkerFactory(
        IServiceLocator serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    public JobWorker CreateWorker(Job job)
    {
        var jobWorker = new JobWorker(_serviceLocator);
        jobWorker.WithJob(job);
        return jobWorker;
    }
}
