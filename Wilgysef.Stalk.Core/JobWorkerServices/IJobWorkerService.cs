using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.JobWorkerManagers;

public interface IJobWorkerService : ISingletonDependency
{
    IReadOnlyCollection<JobWorker> Workers { get; }

    IReadOnlyCollection<Job> Jobs { get; }

    bool CanStartAdditionalWorkers { get; }

    Task<bool> StartJobWorker(Job job);

    Task<bool> StopJobWorker(Job job);
}
