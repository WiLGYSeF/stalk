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

    public async Task ExecuteJobs(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceLocator.BeginLifetimeScope();
        var backgroundJobManager = scope.GetRequiredService<IBackgroundJobManager>();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var job = await backgroundJobManager.GetNextPriorityJobAsync(cancellationToken);
            if (job == null)
            {
                break;
            }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await ExecuteJob(job, cancellationToken);
                await backgroundJobManager.DeleteJobAsync(job);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidBackgroundJobException)
            {
                // TODO: handle invalid background job
                job.SetJobFailed();
                await backgroundJobManager.UpdateJobAsync(job, CancellationToken.None);
            }
            catch (Exception)
            {
                job.SetJobFailed();
                await backgroundJobManager.UpdateJobAsync(job, CancellationToken.None);
            }
        }
    }

    private async Task ExecuteJob(BackgroundJob job, CancellationToken cancellationToken)
    {
        var argsType = job.GetJobArgsType();
        var args = job.DeserializeArgs();

        var eventHandlerType = typeof(IBackgroundJobHandler<>);
        var genericType = eventHandlerType.MakeGenericType(argsType);

        var genericEnumerableType = typeof(IEnumerable<>).MakeGenericType(genericType);
        var services = _serviceLocator.GetRequiredService(genericEnumerableType);

        var handlerWrapper = (IBackgroundJobHandlerWrapper)Activator.CreateInstance(
            typeof(BackgroundJobHandlerWrapper<>).MakeGenericType(argsType))!;

        await handlerWrapper.ExecuteJobAsync(services, args, cancellationToken);
    }
}
