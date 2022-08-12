using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Core.BackgroundJobs;

public class BackgroundJobDispatcher : IBackgroundJobDispatcher
{
    private readonly IServiceLocator _serviceLocator;

    public BackgroundJobDispatcher(IServiceLocator serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    public async Task DispatchJobs<T>(params T[] args) where T : notnull, BackgroundJobArgs
    {
        var eventHandlerType = typeof(IBackgroundJobHandler<>);

        foreach (var arg in args)
        {
            var dataType = arg.GetType();
            var genericType = eventHandlerType.MakeGenericType(dataType);

            var genericEnumerableType = typeof(IEnumerable<>).MakeGenericType(genericType);
            var services = _serviceLocator.GetRequiredService(genericEnumerableType);

            var handlerWrapper = (IBackgroundJobHandlerWrapper)Activator.CreateInstance(
                typeof(BackgroundJobHandlerWrapper<>).MakeGenericType(dataType))!;

            await handlerWrapper.ExecuteJobAsync(services, arg);
        }
    }
}
