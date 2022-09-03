using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.BackgroundJobs;

public interface IBackgroundJobManager : ITransientDependency
{
    /// <summary>
    /// Enqueues a background job.
    /// </summary>
    /// <param name="job">Background job.</param>
    /// <param name="saveChanges">Whether to save changes immediately after adding background job.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<BackgroundJob> EnqueueJobAsync(BackgroundJob job, bool saveChanges, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enqueues a background job, replacing all queued background jobs of the same type.
    /// </summary>
    /// <param name="job">Background job.</param>
    /// <param name="saveChanges">Whether to save changes immediately after adding background job.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<BackgroundJob> EnqueueOrReplaceJobAsync(BackgroundJob job, bool saveChanges, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enqueues a background job, replacing all queued background jobs that match with <paramref name="compareTo"/>.
    /// </summary>
    /// <param name="job">Background job.</param>
    /// <param name="saveChanges">Whether to save changes immediately after adding background job.</param>
    /// <param name="compareTo">Background job args comparison function.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<BackgroundJob> EnqueueOrReplaceJobAsync(
        BackgroundJob job,
        bool saveChanges,
        Func<BackgroundJobArgs, BackgroundJobArgs, bool> compareTo,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds background job by Id.
    /// </summary>
    /// <param name="id">Background job Id.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Background job, or <see langword="null"/> if no background job was found.</returns>
    Task<BackgroundJob?> FindJobAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets list of background jobs.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>List of background jobs.</returns>
    Task<List<BackgroundJob>> GetJobsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets next priority queued background job.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>Background job, or <see langword="null"/> if there are no queued background jobs.</returns>
    Task<BackgroundJob?> GetNextPriorityJobAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Abandon expired background jobs.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>Background jobs that have been abandoned.</returns>
    Task<List<BackgroundJob>> AbandonExpiredJobsAsync(CancellationToken cancellationToken = default);

    Task UpdateJobAsync(BackgroundJob job, CancellationToken cancellationToken = default);

    Task DeleteJobAsync(BackgroundJob job, CancellationToken cancellationToken = default);
}
