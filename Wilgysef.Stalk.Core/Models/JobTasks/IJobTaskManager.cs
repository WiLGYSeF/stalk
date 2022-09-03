using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.Models.JobTasks;

public interface IJobTaskManager : ITransientDependency
{
    /// <summary>
    /// Creates a job task.
    /// </summary>
    /// <param name="jobTask">Job task.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<JobTask> CreateJobTaskAsync(JobTask jobTask, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates job tasks.
    /// </summary>
    /// <param name="jobTasks">Job tasks.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task CreateJobTasksAsync(IEnumerable<JobTask> jobTasks, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a job task by its Id.
    /// </summary>
    /// <param name="id">Job task Id.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Job.</returns>
    Task<JobTask> GetJobTaskAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates job task.
    /// </summary>
    /// <param name="jobTask">Job task.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<JobTask> UpdateJobTaskAsync(JobTask jobTask, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes job task.
    /// </summary>
    /// <param name="jobTask">Job task.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task DeleteJobTaskAsync(JobTask jobTask, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets job task as active.
    /// </summary>
    /// <param name="jobTask">Job task.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task SetJobTaskActiveAsync(JobTask jobTask, CancellationToken cancellationToken = default);
}
