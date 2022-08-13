using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.BackgroundJobs;

public interface IBackgroundJobDispatcher : ISingletonDependency
{
    public IReadOnlyCollection<BackgroundJob> ActiveJobs { get; }

    Task ExecuteJobs(CancellationToken cancellationToken = default);
}
