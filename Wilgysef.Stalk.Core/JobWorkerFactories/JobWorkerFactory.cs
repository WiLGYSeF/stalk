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

    public IJobWorker CreateWorker(Job job)
    {
        return new JobWorker(_serviceLocator.BeginLifetimeScopeFromRoot())
            .WithJob(job);
    }
}
