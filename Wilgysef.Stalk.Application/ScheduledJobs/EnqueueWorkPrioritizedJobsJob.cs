using Quartz;
using Wilgysef.Stalk.Core.BackgroundJobs;
using Wilgysef.Stalk.Core.BackgroundJobs.Args;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.IdGenerators;

namespace Wilgysef.Stalk.Application.ScheduledJobs;

// TODO: have domain event when DelayedUntil expires
/// <summary>
/// This job ensures jobs that have been paused with <see cref="Job.DelayedUntil"/> or <see cref="JobTask.DelayedUntil"/>
/// are worked since there is no domain event when the delay time expires.
/// </summary>
public class EnqueueWorkPrioritizedJobsJob : IJob
{
    private readonly IBackgroundJobManager _backgroundJobManager;
    private readonly IIdGenerator<long> _idGenerator;

    public EnqueueWorkPrioritizedJobsJob(
        IBackgroundJobManager backgroundJobManager,
        IIdGenerator<long> idGenerator)
    {
        _backgroundJobManager = backgroundJobManager;
        _idGenerator = idGenerator;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await _backgroundJobManager.EnqueueOrReplaceJobAsync(
            BackgroundJob.Create(
                _idGenerator.CreateId(),
                new WorkPrioritizedJobsArgs(),
                maximumAttempts: 2),
            true);
    }
}
