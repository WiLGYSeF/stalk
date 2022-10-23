namespace Wilgysef.Stalk.Core.Models.JobTasks;

public interface IJobTaskManager
{
    /// <summary>
    /// Creates a job task.
    /// </summary>
    /// <param name="jobTask">Job task.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task<JobTask> CreateJobTaskAsync(JobTask jobTask, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates job tasks.
    /// </summary>
    /// <param name="jobTasks">Job tasks.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task CreateJobTasksAsync(IEnumerable<JobTask> jobTasks, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a job task by its Id.
    /// </summary>
    /// <param name="id">Job task Id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Job.</returns>
    Task<JobTask> GetJobTaskAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates job task.
    /// </summary>
    /// <param name="jobTask">Job task.</param>
    /// <param name="forceUpdate">Indicates if the job task's properties should be force updated.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task<JobTask> UpdateJobTaskAsync(JobTask jobTask, bool forceUpdate = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes job task.
    /// </summary>
    /// <param name="jobTask">Job task.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task DeleteJobTaskAsync(JobTask jobTask, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets job task as active.
    /// </summary>
    /// <param name="jobTask">Job task.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task SetJobTaskActiveAsync(JobTask jobTask, CancellationToken cancellationToken = default);
}
