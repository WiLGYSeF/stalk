using Wilgysef.Stalk.Core.BackgroundJobs;

namespace Wilgysef.Stalk.Core.BackgroundJobs;

internal interface IBackgroundJobHandlerWrapper
{
    Task ExecuteJobAsync(object handlers, object args);
}

internal class BackgroundJobHandlerWrapper<T> : IBackgroundJobHandlerWrapper where T : BackgroundJobArgs
{
    public async Task ExecuteJobAsync(object handlers, object args)
    {
        foreach (var handler in (IEnumerable<IBackgroundJobHandler<T>>)handlers)
        {
            await handler.ExecuteJobAsync((T)args);
        }
    }
}
