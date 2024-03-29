﻿using Shouldly;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Exceptions;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Core.Tests.JobStateManagerTests;

public class UnpauseJobTaskAsyncTest : BaseTest
{
    private readonly IJobManager _jobManager;
    private readonly IJobTaskStateManager _jobTaskStateManager;

    public UnpauseJobTaskAsyncTest()
    {
        _jobManager = GetRequiredService<IJobManager>();
        _jobTaskStateManager = GetRequiredService<IJobTaskStateManager>();
    }

    [Theory]
    [InlineData(JobTaskState.Active, false, false)]
    [InlineData(JobTaskState.Inactive, false, false)]
    [InlineData(JobTaskState.Completed, false, true)]
    [InlineData(JobTaskState.Failed, false, true)]
    [InlineData(JobTaskState.Cancelled, false, true)]
    [InlineData(JobTaskState.Cancelling, false, true)]
    [InlineData(JobTaskState.Paused, true, false)]
    [InlineData(JobTaskState.Pausing, false, true)]
    public async Task Unpause_JobTask(JobTaskState state, bool change, bool throwsException)
    {
        var job = new JobBuilder().WithRandomInitializedState(JobState.Active)
            .WithRandomTasks(state, 1)
            .Create();
        var jobTask = job.Tasks.First();

        await _jobManager.CreateJobAsync(job);

        if (throwsException)
        {
            await Should.ThrowAsync<BusinessException>(async () => await _jobTaskStateManager.UnpauseJobTaskAsync(jobTask));
            return;
        }

        await _jobTaskStateManager.UnpauseJobTaskAsync(jobTask);
        job = await _jobManager.GetJobAsync(job.Id);
        jobTask = job.Tasks.First();

        if (change)
        {
            jobTask.State.ShouldBe(JobTaskState.Inactive);
        }
        else
        {
            jobTask.State.ShouldBe(state);
        }
    }
}
