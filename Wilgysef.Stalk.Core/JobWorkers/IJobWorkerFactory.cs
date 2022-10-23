using Wilgysef.Stalk.Core.Models.Jobs;

namespace Wilgysef.Stalk.Core.JobWorkers;

public interface IJobWorkerFactory
{
    /// <summary>
    /// Creates a job worker.
    /// </summary>
    /// <param name="job">Job.</param>
    /// <returns>Job worker.</returns>
    IJobWorker CreateWorker(Job job);
}
