﻿using Moq;
using Shouldly;
using Wilgysef.Stalk.Core.JobWorkerServices;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Exceptions;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Core.Tests.JobStateManagerTests;

public class StopJobAsyncTest : BaseTest
{
    private readonly IJobManager _jobManager;
    private readonly IJobStateManager _jobStateManager;

    private readonly ManualResetEventSlim _manualResetEventSlimOuter = new();
    private readonly ManualResetEventSlim _manualResetEventSlimInner = new();

    public StopJobAsyncTest()
    {
        var jobWorkerService = new Mock<IJobWorkerService>();

        jobWorkerService.Setup(s => s.StopJobWorkerAsync(It.IsAny<Job>())).Callback(() =>
        {
            _manualResetEventSlimInner.Set();
            _manualResetEventSlimOuter.Wait();
        });

        ReplaceService(_ => jobWorkerService.Object);

        _jobManager = GetRequiredService<IJobManager>();
        _jobStateManager = GetRequiredService<IJobStateManager>();
    }

    [Theory]
    [InlineData(JobState.Active, true, true, false)]
    [InlineData(JobState.Inactive, true, true, false)]
    [InlineData(JobState.Completed, false, false, false)]
    [InlineData(JobState.Failed, false, false, false)]
    [InlineData(JobState.Cancelled, false, false, false)]
    [InlineData(JobState.Cancelling, false, false, true)]
    [InlineData(JobState.Paused, false, false, false)]
    [InlineData(JobState.Pausing, false, false, true)]
    public async Task Stop_Job(JobState state, bool intermediaryChange, bool change, bool throwsException)
    {
        var job = new JobBuilder().WithRandomInitializedState(state).Create();
        await _jobManager.CreateJobAsync(job);

        if (throwsException)
        {
            await Should.ThrowAsync<BusinessException>(async () => await _jobStateManager.StopJobAsync(job));
            return;
        }

        var task = Task.Run(async () => await _jobStateManager.StopJobAsync(job));

        // timeout
        var setTask = Task.Run(async () =>
        {
            await Task.Delay(200);
            _manualResetEventSlimInner.Set();
        });

        _manualResetEventSlimInner.Wait();

        if (intermediaryChange)
        {
            job = await _jobManager.GetJobAsync(job.Id);

            try
            {
                job.State.ShouldBe(JobState.Cancelling);
            }
            catch
            {
                job.State.ShouldBe(JobState.Cancelled);
            }
        }

        _manualResetEventSlimOuter.Set();
        await task;

        job = await _jobManager.GetJobAsync(job.Id);

        if (change)
        {
            job.State.ShouldBe(JobState.Cancelled);
        }
        else
        {
            if (state == JobState.Paused)
            {
                job.State.ShouldBe(JobState.Cancelled);
            }
            else
            {
                job.State.ShouldBe(state);
            }
        }
    }
}
