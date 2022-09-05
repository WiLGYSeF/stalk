using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Wilgysef.Stalk.Application.ServiceRegistrar;
using Wilgysef.Stalk.Core.BackgroundJobs;
using Wilgysef.Stalk.Core.BackgroundJobs.Args;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.IdGenerators;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Application;

public class Startup
{
    private readonly IRootLifetimeScopeService _rootLifetimeScope;
    private readonly IJobManager _jobManager;
    private readonly IBackgroundJobManager _backgroundJobManager;
    private readonly IIdGenerator<long> _idGenerator;

    private readonly CancellationTokenSource _backgroundTaskTokenSource = new();

    private Task _backgroundJobTask;

    private bool _started = false;

    public Startup(
        IRootLifetimeScopeService rootLifetimeScope,
        IJobManager jobManager,
        IBackgroundJobManager backgroundJobManager,
        IIdGenerator<long> idGenerator)
    {
        _rootLifetimeScope = rootLifetimeScope;
        _jobManager = jobManager;
        _backgroundJobManager = backgroundJobManager;
        _idGenerator = idGenerator;
    }

    /// <summary>
    /// Initialize the application.
    /// </summary>
    /// <param name="rootLifetimeScope">Root lifetime scope.</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException">Application was already started.</exception>
    public async Task StartAsync(ILifetimeScope rootLifetimeScope)
    {
        if (_started)
        {
            throw new InvalidOperationException("Already started.");
        }
        _started = true;

        _rootLifetimeScope.LifetimeScope = rootLifetimeScope;

        // deactivate any jobs that may have an active state from an unclean shutdown
        await _jobManager.DeactivateJobsAsync();

        // enqueue a job to start working on queued jobs that may be present
        await _backgroundJobManager.EnqueueOrReplaceJobAsync(
            BackgroundJob.Create(
                _idGenerator.CreateId(),
                new WorkPrioritizedJobsArgs(),
                maximumAttempts: 2),
            true);

        // start the background job dispatcher
        _backgroundJobTask = Task.Run(
            async () => await DispatchBackgroundJobsAsync(5 * 1000, _backgroundTaskTokenSource.Token),
            _backgroundTaskTokenSource.Token);
    }

    private async Task DispatchBackgroundJobsAsync(int interval, CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(interval, cancellationToken);

            using var scope = _rootLifetimeScope.BeginLifetimeScope();
            var backgroundJobDispatcher = scope.GetRequiredService<IBackgroundJobDispatcher>();

            cancellationToken.ThrowIfCancellationRequested();
            await backgroundJobDispatcher.ExecuteJobsAsync(cancellationToken);
        }
    }
}
