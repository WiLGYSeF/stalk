using Moq;
using Shouldly;
using Wilgysef.Stalk.Core.JobWorkerManagers;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Exceptions;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Core.Tests.JobStateManagerTests;

public class UnpauseJobAsyncTest : BaseTest
{
    private readonly IJobManager _jobManager;
    private readonly IJobStateManager _jobStateManager;

    public UnpauseJobAsyncTest()
    {
        _jobManager = GetRequiredService<IJobManager>();
        _jobStateManager = GetRequiredService<IJobStateManager>();
    }

    [Fact]
    public async Task Unpause_Job()
    {
        var job = new JobBuilder().WithRandomInitializedState(JobState.Paused).Create();

        await _jobManager.CreateJobAsync(job);

        await _jobStateManager.UnpauseJobAsync(job);

        job = await _jobManager.GetJobAsync(job.Id);
        job.State.ShouldBe(JobState.Inactive);
    }

    [Fact]
    public async Task Unpause_Done_Job()
    {
        var job = new JobBuilder().WithRandomInitializedState(JobState.Completed).Create();

        await _jobManager.CreateJobAsync(job);

        await Should.ThrowAsync<JobAlreadyDoneException>(_jobStateManager.UnpauseJobAsync(job));
    }

    [Fact]
    public async Task Unpause_Job_Inactive()
    {
        var job = new JobBuilder().WithRandomInitializedState(JobState.Inactive).Create();

        await _jobManager.CreateJobAsync(job);

        await _jobStateManager.UnpauseJobAsync(job);

        job = await _jobManager.GetJobAsync(job.Id);
        job.State.ShouldBe(JobState.Inactive);
    }
}
