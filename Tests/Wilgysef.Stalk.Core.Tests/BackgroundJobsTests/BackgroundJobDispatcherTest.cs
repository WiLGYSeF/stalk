using Shouldly;
using Wilgysef.Stalk.Core.BackgroundJobs;
using Wilgysef.Stalk.Core.BackgroundJobs.Args;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Core.Tests.BackgroundJobsTests;

public class BackgroundJobDispatcherTest : BaseTest
{
    private readonly IBackgroundJobDispatcher _backgroundJobDispatcher;
    private readonly IBackgroundJobManager _backgroundJobManager;

    public BackgroundJobDispatcherTest()
    {
        _backgroundJobDispatcher = GetRequiredService<IBackgroundJobDispatcher>();
        _backgroundJobManager = GetRequiredService<IBackgroundJobManager>();
    }

    [Fact]
    public async Task Executes_Jobs()
    {
        await _backgroundJobManager.EnqueueJobAsync(
            BackgroundJob.Create(
                1,
                new WorkPrioritizedJobsArgs()),
            true);
        await _backgroundJobManager.EnqueueJobAsync(
            BackgroundJob.Create(
                2,
                new WorkPrioritizedJobsArgs()),
            true);

        var jobs = await _backgroundJobManager.GetJobsAsync();
        jobs.Count.ShouldBe(2);

        await _backgroundJobDispatcher.ExecuteJobsAsync();

        jobs = await _backgroundJobManager.GetJobsAsync();
        jobs.ShouldBeEmpty();
    }

    // TODO: test
    //[Fact]
    //public async Task Executes_Failed_Job()
    //{
    //}
}
