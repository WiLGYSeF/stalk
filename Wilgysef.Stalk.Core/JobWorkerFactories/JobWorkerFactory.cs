using Microsoft.Extensions.Logging;
using Wilgysef.Stalk.Core.JobScopeServices;
using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.JobWorkerFactories;

public class JobWorkerFactory : IJobWorkerFactory, ITransientDependency
{
    public ILogger? Logger { get; set; }

    private readonly IJobScopeService _jobScopeService;

    public JobWorkerFactory(
        IJobScopeService jobScopeService)
    {
        _jobScopeService = jobScopeService;
    }

    public IJobWorker CreateWorker(Job job)
    {
        return new JobWorker(
            _jobScopeService.GetJobScope(job.Id),
            job)
        {
            Logger = Logger,
        };
    }
}
