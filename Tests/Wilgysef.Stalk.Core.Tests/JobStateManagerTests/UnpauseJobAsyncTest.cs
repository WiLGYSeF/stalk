using Shouldly;
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

    [Theory]
    [InlineData(JobState.Active, false, false)]
    [InlineData(JobState.Inactive, false, false)]
    [InlineData(JobState.Completed, false, true)]
    [InlineData(JobState.Failed, false, true)]
    [InlineData(JobState.Cancelled, false, true)]
    [InlineData(JobState.Cancelling, false, false)]
    [InlineData(JobState.Paused, true, false)]
    [InlineData(JobState.Pausing, false, false)]
    public async Task Unpause_Job(JobState state, bool change, bool throwsException)
    {
        var job = new JobBuilder().WithRandomInitializedState(state).Create();

        await _jobManager.CreateJobAsync(job);

        if (throwsException)
        {
            await Should.ThrowAsync<JobAlreadyDoneException>(_jobStateManager.UnpauseJobAsync(job));
            return;
        }

        await _jobStateManager.UnpauseJobAsync(job);
        job = await _jobManager.GetJobAsync(job.Id);

        if (change)
        {
            job.State.ShouldBe(JobState.Inactive);
        }
        else
        {
            job.State.ShouldBe(state);
        }
    }
}
