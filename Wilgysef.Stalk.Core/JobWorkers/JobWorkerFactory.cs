using Microsoft.Extensions.Logging;
using Wilgysef.Stalk.Core.JobScopeServices;
using Wilgysef.Stalk.Core.Loggers;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.ObjectInstances;
using Wilgysef.Stalk.Core.Shared.Dependencies;
using ILoggerFactory = Wilgysef.Stalk.Core.Shared.Loggers.ILoggerFactory;

namespace Wilgysef.Stalk.Core.JobWorkers;

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
        var jobConfig = job.GetConfig();

        IObjectInstanceHandle<ILogger>? loggerHandle = null;
        var jobLogger = Logger;

        if (jobConfig.Logs?.Path != null)
        {
            loggerHandle = _loggerCollectionService.GetLoggerHandle(
                jobConfig.Logs.Path,
                () => _loggerFactory.CreateLogger(jobConfig.Logs.Path, (LogLevel)jobConfig.Logs.Level));
            jobLogger = new AggregateLogger(Logger, loggerHandle.Value);
        }

        var jobScope = _jobScopeService.GetJobScope(job.Id);
        _jobScopeService.AddJobLogger(job.Id, jobLogger);

        var jobWorker = new JobWorker(
            jobScope,
            jobLogger,
            job);

        if (loggerHandle != null)
        {
            jobWorker.OnDisposed += (_, _) => loggerHandle.Dispose();
        }

        return jobWorker;
    }
}
