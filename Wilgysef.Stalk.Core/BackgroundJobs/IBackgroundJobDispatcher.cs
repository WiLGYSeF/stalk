namespace Wilgysef.Stalk.Core.BackgroundJobs;

public interface IBackgroundJobDispatcher
{
    /// <summary>
    /// Executes queued background jobs.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task ExecuteJobsAsync(CancellationToken cancellationToken = default);
}
