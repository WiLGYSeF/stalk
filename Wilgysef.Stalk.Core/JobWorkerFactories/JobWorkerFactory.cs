using Microsoft.Extensions.Logging;
using Wilgysef.Stalk.Core.JobScopeServices;
using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.Loggers;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Dependencies;
using ILoggerFactory = Wilgysef.Stalk.Core.Shared.Loggers.ILoggerFactory;

namespace Wilgysef.Stalk.Core.JobWorkerFactories;

public class JobWorkerFactory : IJobWorkerFactory, ITransientDependency
{
    public ILogger? Logger { get; set; }

    private readonly IJobScopeService _jobScopeService;
    private readonly ILoggerCollectionService _loggerCollectionService;
    private readonly ILoggerFactory _loggerFactory;

    public JobWorkerFactory(
        IJobScopeService jobScopeService,
        ILoggerCollectionService loggerCollectionService,
        ILoggerFactory loggerFactory)
    {
        _jobScopeService = jobScopeService;
        _loggerCollectionService = loggerCollectionService;
        _loggerFactory = loggerFactory;
    }

    public IJobWorker CreateWorker(Job job)
    {
        return new JobWorker(
            _jobScopeService.GetJobScope(job.Id),
            _loggerCollectionService,
            _loggerFactory,
            job)
        {
            Logger = Logger,
        };
    }
}
