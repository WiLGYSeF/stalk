using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.BackgroundJobs;

public interface IBackgroundJobDispatcher : ISingletonDependency
{
    /// <summary>
    /// Active background jobs.
    /// </summary>
    public IReadOnlyCollection<BackgroundJob> ActiveJobs { get; }

    /// <summary>
    /// Executes queued background jobs.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task ExecuteJobs(CancellationToken cancellationToken = default);
}
