using Wilgysef.Stalk.Core.Models.JobTasks;

namespace Wilgysef.Stalk.Core.JobTaskWorkerServices;

public interface IJobTaskWorkerService
{
    /// <summary>
    /// Starts a job task worker with the job task.
    /// </summary>
    /// <param name="jobTask">Job task.</param>
    /// <param name="jobCancellationToken">Cancellation token that <paramref name="job"/> uses.</param>
    /// <returns>Job task worker task.</returns>
    Task<Task> StartJobTaskWorkerAsync(JobTask jobTask, CancellationToken jobCancellationToken);

    /// <summary>
    /// Stops a job task worker. Awaits until the job task is no longer active.
    /// </summary>
    /// <param name="jobTask">Job task.</param>
    /// <returns></returns>
    Task<bool> StopJobTaskWorkerAsync(JobTask job);
}
