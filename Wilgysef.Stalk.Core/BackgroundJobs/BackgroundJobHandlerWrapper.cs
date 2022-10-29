namespace Wilgysef.Stalk.Core.BackgroundJobs;

internal interface IBackgroundJobHandlerWrapper
{
    Task ExecuteJobAsync(object handlers, object args, BackgroundJob job, CancellationToken cancellationToken);
}

internal class BackgroundJobHandlerWrapper<T> : IBackgroundJobHandlerWrapper where T : BackgroundJobArgs
{
    public async Task ExecuteJobAsync(object handlers, object args, BackgroundJob job, CancellationToken cancellationToken)
    {
        foreach (var handler in (IEnumerable<IBackgroundJobHandler<T>>)handlers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            handler.BackgroundJob = job;
            await handler.ExecuteJobAsync((T)args, cancellationToken);
        }
    }
}
