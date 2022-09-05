using Microsoft.Extensions.Logging;
using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Core.JobWorkerFactories;

public class JobWorkerFactory : IJobWorkerFactory
{
    public ILogger? Logger { get; set; }

    private readonly IServiceLocator _serviceLocator;

    public JobWorkerFactory(
        IServiceLocator serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    public IJobWorker CreateWorker(Job job)
    {
        return new JobWorker(_serviceLocator.BeginLifetimeScopeFromRoot())
        {
            Logger = Logger,
        }
            .WithJob(job);
    }
}
