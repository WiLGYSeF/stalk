using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.JobWorkerManagers;

public interface IJobTaskWorkerService : ISingletonDependency
{
    bool StartJobTaskWorker(JobTask task);

    Task<bool> StopJobTaskWorker(JobTask task);
}
