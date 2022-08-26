using Wilgysef.Stalk.Core.JobWorkerFactories;
using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.TestBase.Mocks;

public class JobWorkerFactoryMock : IJobWorkerFactory
{
    private readonly IServiceLocator _serviceLocator;

    public JobWorkerFactoryMock(
        IServiceLocator serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    public IJobWorker CreateWorker(Job job)
    {
        return new JobWorkerMock(_serviceLocator.BeginLifetimeScope())
            .WithJob(job);
    }
}
