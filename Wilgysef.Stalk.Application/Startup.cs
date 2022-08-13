﻿using Wilgysef.Stalk.Core.BackgroundJobs;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Application;

public class Startup
{
    private readonly IJobManager _jobManager;
    private readonly IServiceLocator _serviceLocator;

    private readonly CancellationTokenSource _backgroundTaskTokenSource = new();

    private Task _backgroundJobTask;

    private bool _started = false;

    public Startup(
        IJobManager jobManager,
        IServiceLocator serviceLocator)
    {
        _jobManager = jobManager;
        _serviceLocator = serviceLocator;
    }

    public async Task StartAsync()
    {
        if (_started)
        {
            throw new InvalidOperationException("Already started.");
        }
        _started = true;

        await _jobManager.DeactivateJobsAsync();

        _backgroundJobTask = new Task(
            async () => await DispatchBackgroundJobs(_backgroundTaskTokenSource.Token),
            _backgroundTaskTokenSource.Token,
            TaskCreationOptions.LongRunning);
        _backgroundJobTask.Start();
    }

    private async Task DispatchBackgroundJobs(CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(5 * 1000, cancellationToken);

            using var scope = _serviceLocator.BeginLifetimeScope();
            var backgroundJobDispatcher = scope.GetRequiredService<IBackgroundJobDispatcher>();

            cancellationToken.ThrowIfCancellationRequested();
            await backgroundJobDispatcher.ExecuteJobs(cancellationToken);
        }
    }
}