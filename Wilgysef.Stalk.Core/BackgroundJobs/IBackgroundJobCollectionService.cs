namespace Wilgysef.Stalk.Core.BackgroundJobs;

public interface IBackgroundJobCollectionService
{
    /// <summary>
    /// Active background jobs.
    /// </summary>
    IReadOnlyCollection<BackgroundJob> ActiveJobs { get; }

    /// <summary>
    /// Adds an active background job.
    /// </summary>
    /// <param name="job">Active background job.</param>
    void AddActiveJob(BackgroundJob job);

    /// <summary>
    /// Removes an active background job.
    /// </summary>
    /// <param name="job">Active background job.</param>
    void RemoveActiveJob(BackgroundJob job);
}
