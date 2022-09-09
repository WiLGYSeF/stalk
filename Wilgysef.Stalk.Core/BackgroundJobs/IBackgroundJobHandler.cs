namespace Wilgysef.Stalk.Core.BackgroundJobs;

public interface IBackgroundJobHandler<T> where T : BackgroundJobArgs
{
    /// <summary>
    /// The background job being executed.
    /// </summary>
    BackgroundJob BackgroundJob { get; internal set; }

    /// <summary>
    /// Executes background job.
    /// </summary>
    /// <param name="args">Background job args.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task ExecuteJobAsync(T args, CancellationToken cancellationToken = default);
}
