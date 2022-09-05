using Microsoft.Extensions.Logging;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Core.BackgroundJobs;

public class BackgroundJobDispatcher : IBackgroundJobDispatcher
{
    public ILogger? Logger { get; set; }

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

            var abandonedJobs = await backgroundJobManager.AbandonExpiredJobsAsync(cancellationToken);
            if (abandonedJobs.Count > 0)
            {
                Logger?.LogInformation("Abandoning expired jobs, {JOB_COUNT} job(s) abandoned.", abandonedJobs.Count);
            }

            var job = await backgroundJobManager.GetNextPriorityJobAsync(cancellationToken);
            if (job == null)
            {
                break;
            }

            cancellationToken.ThrowIfCancellationRequested();
            Logger?.LogInformation("Background job {JOB_ID} {JOB_ARGSNAME} starting.", job.Id, job.JobArgsName);
            Logger?.LogDebug("Background job {JOB_ID} {JOB_ARGSNAME} attempt {JOB_ATTEMPT} starting.", job.Id, job.JobArgsName, job.Attempts);

            try
            {
                _backgroundJobCollectionService.AddActiveJob(job);
                await ExecuteJobAsync(job, cancellationToken);

                Logger?.LogInformation("Background job {JOB_ID} finished.", job.Id);
                await backgroundJobManager.DeleteJobAsync(job, CancellationToken.None);
            }
            catch (InvalidBackgroundJobException exception)
            {
                Logger?.LogError(exception, "Background job {JOB_ID} was invalid.", job.Id);

                job.Abandon();
                await backgroundJobManager.UpdateJobAsync(job, CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
                Logger?.LogInformation("Background job {JOB_ID} cancelled.", job.Id);
                throw;
            }
            catch (Exception exception)
            {
                Logger?.LogError(exception, "Background job {JOB_ID} failed.", job.Id);

                job.JobFailed();
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

        cancellationToken.ThrowIfCancellationRequested();

        await handlerWrapper.ExecuteJobAsync(services, args, job, cancellationToken);
    }
}
