﻿using Moq;
using Shouldly;
using Wilgysef.Stalk.Core.JobWorkerManagers;
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

        jobWorkerService.Setup(s => s.StopJobWorker(It.IsAny<Job>())).Callback(() =>
        {
            _manualResetEventSlimInner.Set();
            _manualResetEventSlimOuter.Wait();
        });

        ReplaceServiceInstance<IJobWorkerService, IJobWorkerService>(jobWorkerService.Object);

        _jobManager = GetRequiredService<IJobManager>();
        _jobStateManager = GetRequiredService<IJobStateManager>();
    }

    [Theory]
    [InlineData(JobState.Active, true, true)]
    [InlineData(JobState.Inactive, true, true)]
    [InlineData(JobState.Completed, false, false)]
    [InlineData(JobState.Failed, false, false)]
    [InlineData(JobState.Cancelled, false, false)]
    [InlineData(JobState.Cancelling, false, false)]
    [InlineData(JobState.Paused, false, false)]
    [InlineData(JobState.Pausing, false, false)]
    public async Task Stop_Job(JobState state, bool intermediaryChange, bool change)
    {
        var job = new JobBuilder().WithRandomInitializedState(state).Create();

        await _jobManager.CreateJobAsync(job);

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