using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.JobWorkerFactories;

public interface IJobWorkerFactory : ITransientDependency
{
    /// <summary>
    /// Creates a job worker.
    /// </summary>
    /// <param name="job">Job.</param>
    /// <returns>Job worker.</returns>
    IJobWorker CreateWorker(Job job);
}
