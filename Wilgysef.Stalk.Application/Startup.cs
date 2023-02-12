using Autofac;
using Quartz;
using Quartz.Spi;
using Wilgysef.Stalk.Application.ScheduledJobs;
using Wilgysef.Stalk.Application.ServiceRegistrar;
using Wilgysef.Stalk.Core.BackgroundJobs;
using Wilgysef.Stalk.Core.BackgroundJobs.Args;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.IdGenerators;

namespace Wilgysef.Stalk.Application;

public class Startup
{
    public TimeSpan BackgroundJobInterval { get; set; } = TimeSpan.FromSeconds(10);

    public TimeSpan EnqueuePrioritizedJobsInterval { get; set; } = TimeSpan.FromMinutes(2);

    private readonly IRootLifetimeScopeService _rootLifetimeScope;
    private readonly IJobManager _jobManager;
    private readonly IBackgroundJobManager _backgroundJobManager;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IJobFactory _scheduleJobFactory;
    private readonly IIdGenerator<long> _idGenerator;

    private readonly CancellationTokenSource _schedulerTokenSource = new();

    private bool _started = false;

    public Startup(
        IRootLifetimeScopeService rootLifetimeScope,
        IJobManager jobManager,
        IBackgroundJobManager backgroundJobManager,
        ISchedulerFactory schedulerFactory,
        IJobFactory scheduleJobFactory,
        IIdGenerator<long> idGenerator)
    {
        _rootLifetimeScope = rootLifetimeScope;
        _jobManager = jobManager;
        _backgroundJobManager = backgroundJobManager;
        _schedulerFactory = schedulerFactory;
        _scheduleJobFactory = scheduleJobFactory;
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

        await StartScheduledJobs();
    }

    private async Task StartScheduledJobs()
    {
        await StartScheduledJob<BackgroundJobDispatcherJob>(
            TriggerBuilder.Create()
                .WithSimpleSchedule(b => b.WithInterval(BackgroundJobInterval).RepeatForever())
                .Build());

        await StartScheduledJob<EnqueueWorkPrioritizedJobsJob>(
            TriggerBuilder.Create()
                .WithSimpleSchedule(b => b.WithInterval(EnqueuePrioritizedJobsInterval).RepeatForever())
                .Build());
    }

    private async Task StartScheduledJob<T>(ITrigger trigger) where T : IJob
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        scheduler.JobFactory = _scheduleJobFactory;

        await scheduler.ScheduleJob(Quartz.JobBuilder.Create<T>().Build(), trigger);
        await scheduler.Start(_schedulerTokenSource.Token);
    }
}
