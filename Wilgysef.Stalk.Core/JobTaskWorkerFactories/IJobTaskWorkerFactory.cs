using Wilgysef.Stalk.Core.JobTaskWorkers;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.JobTaskWorkerFactories;

public interface IJobTaskWorkerFactory : ITransientDependency
{
    /// <summary>
    /// Creates a job task worker.
    /// </summary>
    /// <param name="jobTask">Job task.</param>
    /// <returns>Job task worker.</returns>
    IJobTaskWorker CreateWorker(JobTask jobTask);
}
