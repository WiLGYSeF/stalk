using System.Text.Json;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Core.BackgroundJobs;

public class BackgroundJobDispatcher : IBackgroundJobDispatcher
{
    public IReadOnlyCollection<BackgroundJob> ActiveJobs => _backgroundTasks.Values;

    private readonly IServiceLocator _serviceLocator;

    private readonly Dictionary<Task, BackgroundJob> _backgroundTasks = new();

    public BackgroundJobDispatcher(
        IServiceLocator serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    public async Task ExecuteJobs()
    {
        using var scope = _serviceLocator.BeginLifetimeScope();
        var backgroundJobManager = scope.GetRequiredService<IBackgroundJobManager>();

        while (true)
        {
            var job = await backgroundJobManager.GetNextPriorityJobAsync();
            if (job == null)
            {
                break;
            }

            try
            {
                await ExecuteJob(job);
                await backgroundJobManager.DeleteJobAsync(job);
            }
            catch (InvalidBackgroundJobException exception)
            {

            }
            catch (Exception exception)
            {
                job.SetJobFailed();
                await backgroundJobManager.UpdateJobAsync(job);
            }
        }
    }

    public bool IsJobActive(BackgroundJob job)
    {
        return _backgroundTasks.ContainsValue(job);
    }

    private async Task ExecuteJob(BackgroundJob job)
    {
        var eventHandlerType = typeof(IBackgroundJobHandler<>);
        var argsType = Type.GetType(job.JobArgsName);

        if (argsType == null)
        {
            throw new InvalidBackgroundJobException();
        }

        var args = JsonSerializer.Deserialize(job.JobArgs, argsType);
        if (args == null)
        {
            throw new InvalidBackgroundJobException();
        }

        var genericType = eventHandlerType.MakeGenericType(argsType);

        var genericEnumerableType = typeof(IEnumerable<>).MakeGenericType(genericType);
        var services = _serviceLocator.GetRequiredService(genericEnumerableType);

        var handlerWrapper = (IBackgroundJobHandlerWrapper)Activator.CreateInstance(
            typeof(BackgroundJobHandlerWrapper<>).MakeGenericType(argsType))!;

        await handlerWrapper.ExecuteJobAsync(services, args);
    }
}
