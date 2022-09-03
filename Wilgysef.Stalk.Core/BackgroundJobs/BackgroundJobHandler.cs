namespace Wilgysef.Stalk.Core.BackgroundJobs;

public abstract class BackgroundJobHandler<T> : IBackgroundJobHandler<T> where T : BackgroundJobArgs
{
    public BackgroundJob BackgroundJob { get; set; } = null!;

    public abstract Task ExecuteJobAsync(T args, CancellationToken cancellationToken = default);
}
