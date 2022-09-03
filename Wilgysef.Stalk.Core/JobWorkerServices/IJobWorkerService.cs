using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.JobWorkerServices;

public interface IJobWorkerService : ITransientDependency
{
    /// <summary>
    /// Whether additional workers can be started.
    /// </summary>
    bool CanStartAdditionalWorkers { get; }

    /// <summary>
    /// Starts a job worker with the job.
    /// </summary>
    /// <param name="job">Job.</param>
    /// <returns><see langword="true"/> if job worker was started, otherwise <see langword="false"/>.</returns>
    Task<bool> StartJobWorkerAsync(Job job);

    /// <summary>
    /// Stops a job worker. Awaits until the job is no longer active.
    /// </summary>
    /// <param name="job">Job.</param>
    /// <returns></returns>
    Task<bool> StopJobWorkerAsync(Job job);

    /// <summary>
    /// Gets the active jobs ordered by highest priority, then by least active job taskcount.
    /// </summary>
    /// <returns>Ordered list of active jobs.</returns>
    List<Job> GetJobsByPriority();
}
