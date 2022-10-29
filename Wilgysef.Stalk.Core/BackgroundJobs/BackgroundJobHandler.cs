using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.BackgroundJobs;

public abstract class BackgroundJobHandler<T> : IBackgroundJobHandler<T>, ITransientDependency where T : BackgroundJobArgs
{
    public BackgroundJob BackgroundJob { get; set; } = null!;

    public abstract Task ExecuteJobAsync(T args, CancellationToken cancellationToken = default);
}
