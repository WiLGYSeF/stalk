using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

    private readonly ManualResetEventSlim _manualResetEventSlimOuter = new ManualResetEventSlim();
    private readonly ManualResetEventSlim _manualResetEventSlimInner = new ManualResetEventSlim();

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

    [Fact]
    public async Task Stop_Job()
    {
        var job = new JobBuilder().WithRandomInitializedState(JobState.Active).Create();

        await _jobManager.CreateJobAsync(job);

        var task = Task.Run(async () => await _jobStateManager.StopJobAsync(job));
        _manualResetEventSlimInner.Wait();
        
        job = await _jobManager.GetJobAsync(job.Id);
        job.State.ShouldBe(JobState.Cancelling);
        
        _manualResetEventSlimOuter.Set();
        await task;
        
        job = await _jobManager.GetJobAsync(job.Id);
        job.State.ShouldBe(JobState.Cancelled);
    }

    [Fact]
    public async Task Stop_Done_Job()
    {
        var job = new JobBuilder().WithRandomInitializedState(JobState.Completed).Create();

        await _jobManager.CreateJobAsync(job);

        _manualResetEventSlimOuter.Set();
        await Should.ThrowAsync<JobAlreadyDoneException>(_jobStateManager.StopJobAsync(job));
    }

    [Fact]
    public async Task Stop_Job_Inactive()
    {
        var job = new JobBuilder().WithRandomInitializedState(JobState.Inactive).Create();

        await _jobManager.CreateJobAsync(job);

        _manualResetEventSlimOuter.Set();
        await _jobStateManager.StopJobAsync(job);

        job = await _jobManager.GetJobAsync(job.Id);
        job.State.ShouldBe(JobState.Cancelled);
    }
}
