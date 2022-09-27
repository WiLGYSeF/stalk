using Wilgysef.Stalk.Core.BackgroundJobs.Args;
using Wilgysef.Stalk.Core.BackgroundJobs.Executors;
using Wilgysef.Stalk.Core.JobWorkerServices;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Application.Tests.Utilities;

internal class JobStarter
{
    private readonly IServiceLifetimeScope _scope;

    public JobStarter(IServiceLifetimeScope scope)
    {
        _scope = scope;
    }

    public async Task WorkPrioritizedJobsAsync()
    {
        var workPrioritizedJobs = new WorkPrioritizedJobsJob(
            _scope.GetRequiredService<IJobManager>(),
            _scope.GetRequiredService<IJobWorkerService>());
        await workPrioritizedJobs.ExecuteJobAsync(new WorkPrioritizedJobsArgs());
    }
}
