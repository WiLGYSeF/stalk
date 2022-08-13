using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.JobWorkerManagers;

public interface IJobWorkerService : ISingletonDependency
{
    /// <summary>
    /// Job workers.
    /// </summary>
    IReadOnlyCollection<JobWorker> Workers { get; }

    /// <summary>
    /// Active jobs.
    /// </summary>
    IReadOnlyCollection<Job> Jobs { get; }

    /// <summary>
    /// Whether additional workers can be started.
    /// </summary>
    bool CanStartAdditionalWorkers { get; }

    /// <summary>
    /// Starts a job worker with the job.
    /// </summary>
    /// <param name="job">Job.</param>
    /// <returns><see langword="true"/> if job worker was started, otherwise <see langword="false"/>.</returns>
    Task<bool> StartJobWorker(Job job);

    /// <summary>
    /// Stops a job worker. Awaits until the job is no longer active.
    /// </summary>
    /// <param name="job">Job.</param>
    /// <returns></returns>
    Task<bool> StopJobWorker(Job job);

    /// <summary>
    /// Gets the active jobs ordered by highest priority, then by active job taskcount.
    /// </summary>
    /// <returns>Ordered list of active jobs.</returns>
    List<Job> GetJobsByPriority();
}
