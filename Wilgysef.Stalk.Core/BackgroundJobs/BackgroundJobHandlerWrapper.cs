using Wilgysef.Stalk.Core.BackgroundJobs;

namespace Wilgysef.Stalk.Core.BackgroundJobs;

internal interface IBackgroundJobHandlerWrapper
{
    Task ExecuteJobAsync(object handlers, object args, CancellationToken cancellationToken);
}

internal class BackgroundJobHandlerWrapper<T> : IBackgroundJobHandlerWrapper where T : BackgroundJobArgs
{
    public async Task ExecuteJobAsync(object handlers, object args, CancellationToken cancellationToken)
    {
        foreach (var handler in (IEnumerable<IBackgroundJobHandler<T>>)handlers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await handler.ExecuteJobAsync((T)args, cancellationToken);
        }
    }
}
