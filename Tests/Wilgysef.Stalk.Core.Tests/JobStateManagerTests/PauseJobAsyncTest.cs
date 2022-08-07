using Moq;
using Shouldly;
using Wilgysef.Stalk.Core.JobWorkerManagers;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Exceptions;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Core.Tests.JobStateManagerTests;

public class PauseJobAsyncTest : BaseTest
{
    private readonly IJobManager _jobManager;
    private readonly IJobStateManager _jobStateManager;

    private readonly ManualResetEventSlim _manualResetEventSlimOuter = new ManualResetEventSlim();
    private readonly ManualResetEventSlim _manualResetEventSlimInner = new ManualResetEventSlim();

    public PauseJobAsyncTest()
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
    public async Task Pause_Job()
    {
        var job = new JobBuilder().WithRandomInitializedState(JobState.Active).Create();

        await _jobManager.CreateJobAsync(job);

        var task = Task.Run(async () => await _jobStateManager.PauseJobAsync(job));
        _manualResetEventSlimInner.Wait();

        job = await _jobManager.GetJobAsync(job.Id);
        job.State.ShouldBe(JobState.Pausing);

        _manualResetEventSlimOuter.Set();
        await task;

        job = await _jobManager.GetJobAsync(job.Id);
        job.State.ShouldBe(JobState.Paused);
    }

    [Fact]
    public async Task Pause_Done_Job()
    {
        var job = new JobBuilder().WithRandomInitializedState(JobState.Completed).Create();

        await _jobManager.CreateJobAsync(job);

        _manualResetEventSlimOuter.Set();
        await Should.ThrowAsync<JobAlreadyDoneException>(_jobStateManager.PauseJobAsync(job));
    }

    [Fact]
    public async Task Pause_Job_Inactive()
    {
        var job = new JobBuilder().WithRandomInitializedState(JobState.Inactive).Create();

        await _jobManager.CreateJobAsync(job);

        _manualResetEventSlimOuter.Set();
        await _jobStateManager.PauseJobAsync(job);

        job = await _jobManager.GetJobAsync(job.Id);
        job.State.ShouldBe(JobState.Paused);
    }
}
