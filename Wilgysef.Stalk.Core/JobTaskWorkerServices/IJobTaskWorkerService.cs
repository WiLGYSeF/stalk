using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.JobTaskWorkerServices;

public interface IJobTaskWorkerService : ISingletonDependency
{
    /// <summary>
    /// Starts a job task worker with the job task.
    /// </summary>
    /// <param name="job">Job.</param>
    /// <param name="jobTask">Job task.</param>
    /// <returns><see langword="true"/> if job worker was started, otherwise <see langword="false"/>.</returns>
    Task<bool> StartJobTaskWorkerAsync(Job job, JobTask jobTask);

    /// <summary>
    /// Stops a job task worker. Awaits until the job task is no longer active.
    /// </summary>
    /// <param name="jobTask">Job task.</param>
    /// <returns></returns>
    Task<bool> StopJobTaskWorkerAsync(JobTask job);
}
