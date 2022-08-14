using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Core.BackgroundJobs;

public class BackgroundJobDispatcher : IBackgroundJobDispatcher
{
    private readonly IServiceLocator _serviceLocator;
    private readonly IBackgroundJobCollectionService _backgroundJobCollectionService;

    public BackgroundJobDispatcher(
        IServiceLocator serviceLocator,
        IBackgroundJobCollectionService backgroundJobCollectionService)
    {
        _serviceLocator = serviceLocator;
        _backgroundJobCollectionService = backgroundJobCollectionService;
    }

    public async Task ExecuteJobsAsync(CancellationToken cancellationToken = default)
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
                _backgroundJobCollectionService.AddActiveJob(job);
                await ExecuteJobAsync(job, cancellationToken);
                await backgroundJobManager.DeleteJobAsync(job, CancellationToken.None);
            }
            catch (InvalidBackgroundJobException)
            {
                // TODO: handle invalid background job
                job.SetJobFailed();
                await backgroundJobManager.UpdateJobAsync(job, CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                job.SetJobFailed();
                await backgroundJobManager.UpdateJobAsync(job, CancellationToken.None);
            }
            finally
            {
                _backgroundJobCollectionService.RemoveActiveJob(job);
            }
        }
    }

    private async Task ExecuteJobAsync(BackgroundJob job, CancellationToken cancellationToken)
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
