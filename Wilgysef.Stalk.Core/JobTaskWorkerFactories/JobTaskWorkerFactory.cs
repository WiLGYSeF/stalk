using Microsoft.Extensions.Logging;
using Wilgysef.Stalk.Core.JobScopeServices;
using Wilgysef.Stalk.Core.JobTaskWorkers;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.JobTaskWorkerFactories;

public class JobTaskWorkerFactory : IJobTaskWorkerFactory, ITransientDependency
{
    public ILogger? Logger { get; set; }

    private readonly IJobScopeService _jobScopeService;

    public JobTaskWorkerFactory(
        IJobScopeService jobScopeService)
    {
        _jobScopeService = jobScopeService;
    }

    public IJobTaskWorker CreateWorker(JobTask jobTask)
    {
        return new JobTaskWorker(
            _jobScopeService.GetJobScope(jobTask.JobId),
            _jobScopeService.GetJobLogger(jobTask.JobId),
            jobTask);
    }
}
