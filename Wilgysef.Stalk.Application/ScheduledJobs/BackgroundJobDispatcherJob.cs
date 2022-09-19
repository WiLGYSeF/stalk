using Quartz;
using Wilgysef.Stalk.Core.BackgroundJobs;

namespace Wilgysef.Stalk.Application.ScheduledJobs;

public class BackgroundJobDispatcherJob : IJob
{
    private readonly IBackgroundJobDispatcher _backgroundJobDispatcher;

    public BackgroundJobDispatcherJob(
        IBackgroundJobDispatcher backgroundJobDispatcher)
    {
        _backgroundJobDispatcher = backgroundJobDispatcher;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await _backgroundJobDispatcher.ExecuteJobsAsync();
    }
}
