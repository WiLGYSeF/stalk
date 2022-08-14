using IdGen;
using Wilgysef.Stalk.Core.BackgroundJobs;
using Wilgysef.Stalk.Core.BackgroundJobs.Args;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Application;

public class Startup
{
    private readonly IJobManager _jobManager;
    private readonly IBackgroundJobManager _backgroundJobManager;
    private readonly IIdGenerator<long> _idGenerator;
    private readonly IServiceLocator _serviceLocator;

    private readonly CancellationTokenSource _backgroundTaskTokenSource = new();

    private Task _backgroundJobTask;

    private bool _started = false;

    public Startup(
        IJobManager jobManager,
        IBackgroundJobManager backgroundJobManager,
        IIdGenerator<long> idGenerator,
        IServiceLocator serviceLocator)
    {
        _jobManager = jobManager;
        _backgroundJobManager = backgroundJobManager;
        _idGenerator = idGenerator;
        _serviceLocator = serviceLocator;
    }

    public async Task StartAsync()
    {
        if (_started)
        {
            throw new InvalidOperationException("Already started.");
        }
        _started = true;

        // deactivate any jobs that may have an active state from an unclean shutdown
        await _jobManager.DeactivateJobsAsync();

        // enqueue a job to start working on queued jobs that may be present
        await _backgroundJobManager.EnqueueOrReplaceJobAsync(
            BackgroundJob.Create(
                _idGenerator.CreateId(),
                new WorkPrioritizedJobsArgs(),
                maximumLifespan: TimeSpan.FromSeconds(3)),
            true);

        // start the background job dispatcher
        _backgroundJobTask = new Task(
            async () => await DispatchBackgroundJobsAsync(5 * 1000, _backgroundTaskTokenSource.Token),
            _backgroundTaskTokenSource.Token,
            TaskCreationOptions.LongRunning);
        _backgroundJobTask.Start();
    }

    private async Task DispatchBackgroundJobsAsync(int interval, CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(interval, cancellationToken);

            using var scope = _serviceLocator.BeginLifetimeScope();
            var backgroundJobDispatcher = scope.GetRequiredService<IBackgroundJobDispatcher>();

            cancellationToken.ThrowIfCancellationRequested();
            await backgroundJobDispatcher.ExecuteJobsAsync(cancellationToken);
        }
    }
}
