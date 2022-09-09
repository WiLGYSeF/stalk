using Microsoft.Extensions.Logging;
using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Dependencies;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Core.JobWorkerFactories;

public class JobWorkerFactory : IJobWorkerFactory, ITransientDependency
{
    public ILogger? Logger { get; set; }

    private readonly IServiceLocator _serviceLocator;
    private readonly HttpClient _httpClient;

    public JobWorkerFactory(
        IServiceLocator serviceLocator,
        HttpClient httpClient)
    {
        _serviceLocator = serviceLocator;
        _httpClient = httpClient;
    }

    public IJobWorker CreateWorker(Job job)
    {
        return new JobWorker(
            _serviceLocator.BeginLifetimeScopeFromRoot(),
            _httpClient)
        {
            Logger = Logger,
        }
            .WithJob(job);
    }
}
