using Ardalis.Specification;

namespace Wilgysef.Stalk.Core.Models.Jobs;

public interface IJobManager
{
    /// <summary>
    /// Creates a job.
    /// </summary>
    /// <param name="job">Job.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task<Job> CreateJobAsync(Job job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a job by its Id.
    /// </summary>
    /// <param name="id">Job Id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Job.</returns>
    Task<Job> GetJobAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a job by its Id.
    /// </summary>
    /// <param name="id">Job Id.</param>
    /// <param name="readOnly">Indicates if the entity is intended to be read-only.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Job.</returns>
    Task<Job> GetJobAsync(long id, bool readOnly, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a job by a job task Id.
    /// </summary>
    /// <param name="id">Job task Id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Job.</returns>
    Task<Job> GetJobByTaskIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a list of jobs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of jobs.</returns>
    Task<List<Job>> GetJobsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a list of jobs by specification.
    /// </summary>
    /// <param name="specification">Specification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of jobs.</returns>
    Task<List<Job>> GetJobsAsync(ISpecification<Job> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets next priority queued job.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Job, or <see langword="null"/> if there are no queued jobs.</returns>
    Task<Job?> GetNextPriorityJobAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets next priority queued job.
    /// </summary>
    /// <param name="limit">Job limit, returns all jobs if <see langword="null"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Queued jobs.</returns>
    Task<List<Job>> GetNextPriorityJobsAsync(int? limit = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates job.
    /// </summary>
    /// <param name="job">Job.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task<Job> UpdateJobAsync(Job job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates jobs.
    /// </summary>
    /// <param name="jobs">Jobs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task UpdateJobsAsync(IEnumerable<Job> jobs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes job.
    /// </summary>
    /// <param name="job">Job.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task DeleteJobAsync(Job job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets job as active.
    /// </summary>
    /// <param name="job">Job.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task SetJobActiveAsync(Job job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets all active and transitioning jobs and tasks to their inactive and transitioned states.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task DeactivateJobsAsync(CancellationToken cancellationToken = default);
}
