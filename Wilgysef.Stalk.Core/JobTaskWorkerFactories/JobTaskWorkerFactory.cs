using Microsoft.Extensions.Logging;
using Wilgysef.Stalk.Core.JobTaskWorkers;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Dependencies;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Core.JobTaskWorkerFactories;

public class JobTaskWorkerFactory : IJobTaskWorkerFactory, ITransientDependency
{
    public ILogger? Logger { get; set; }

    private readonly IServiceLocator _serviceLocator;

    public JobTaskWorkerFactory(
        IServiceLocator serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    public IJobTaskWorker CreateWorker(JobTask jobTask)
    {
        return new JobTaskWorker(
            _serviceLocator.BeginLifetimeScopeFromRoot(),
            jobTask)
        {
            Logger = Logger,
        };
    }
}
