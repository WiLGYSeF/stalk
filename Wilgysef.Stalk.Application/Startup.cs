using System.Diagnostics;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Application;

public class Startup
{
    private readonly IJobManager _jobManager;
    private readonly IServiceLocator _serviceLocator;

    private bool _started = false;

    private Task _backgroundJobTask;
    private CancellationTokenSource _backgroundTaskTokenSource = new();

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

        var cancellationToken = _backgroundTaskTokenSource.Token;
        _backgroundJobTask = new Task(
            async () => await DispatchBackgroundJobs(cancellationToken),
            cancellationToken,
            TaskCreationOptions.LongRunning);
        _backgroundJobTask.Start();
    }

    private async Task DispatchBackgroundJobs(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            using var scope = _serviceLocator.BeginLifetimeScope();
            var backgroundJobManager = scope.GetRequiredService<IBackgroundJobManager>();
            await Task.Delay(5 * 1000);
            Debug.WriteLine("aaa");
        }
    }
}
