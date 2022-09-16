using Wilgysef.Stalk.Core.JobTaskWorkers;
using Wilgysef.Stalk.Core.Models.JobTasks;

namespace Wilgysef.Stalk.Core.JobTaskWorkerFactories;

public interface IJobTaskWorkerFactory
{
    /// <summary>
    /// Creates a job task worker.
    /// </summary>
    /// <param name="jobTask">Job task.</param>
    /// <returns>Job task worker.</returns>
    IJobTaskWorker CreateWorker(JobTask jobTask);
}
