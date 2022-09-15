using Microsoft.Extensions.Logging;
using Wilgysef.Stalk.Core.Shared.Dependencies;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Core.BackgroundJobs;

public class BackgroundJobDispatcher : IBackgroundJobDispatcher, ITransientDependency
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
                Logger?.LogInformation("{JobCount} expired job(s) abandoned.", abandonedJobs.Count);
            }

            var job = await backgroundJobManager.GetNextPriorityJobAsync(cancellationToken);
            if (job == null)
            {
                break;
            }

            cancellationToken.ThrowIfCancellationRequested();
            Logger?.LogInformation("Background job {JobId} {JobArgsName} starting.", job.Id, job.JobArgsName);
            Logger?.LogDebug("Background job {JobId} {JobArgsName} starting attempt {Attempts} / {MaxAttempts}.", job.Id, job.JobArgsName, job.Attempts, job.MaxAttempts);

            try
            {
                _backgroundJobCollectionService.AddActiveJob(job);
                await ExecuteJobAsync(job, cancellationToken);

                Logger?.LogInformation("Background job {JobId} finished.", job.Id);
                await backgroundJobManager.DeleteJobAsync(job, CancellationToken.None);
            }
            catch (InvalidBackgroundJobException exception)
            {
                Logger?.LogError(exception, "Background job {JobId} was invalid.", job.Id);

                job.Abandon();
                await backgroundJobManager.UpdateJobAsync(job, CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
                Logger?.LogInformation("Background job {JobId} task cancelled.", job.Id);
                throw;
            }
            catch (Exception exception)
            {
                try
                {
                    job.JobFailed();
                    Logger?.LogError(exception, "Background job {JobId} failed, next run is at {NextRun}", job.Id, job.NextRun);
                }
                catch (Exception innerException)
                {
                    Logger?.LogError(innerException, "Background job {JobId} threw exception while failing, abandoning.", job.Id);
                    job.Abandon();
                }

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
