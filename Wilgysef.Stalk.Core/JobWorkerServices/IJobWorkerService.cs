using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.JobWorkerManagers;

public interface IJobWorkerService : ISingletonDependency
{
    bool StartJobWorker(Job job);

    Task<bool> StopJobWorker(Job job);
}
