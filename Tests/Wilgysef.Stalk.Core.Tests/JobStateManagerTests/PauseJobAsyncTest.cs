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

    private readonly ManualResetEventSlim _manualResetEventSlimOuter = new();
    private readonly ManualResetEventSlim _manualResetEventSlimInner = new();

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

    [Theory]
    [InlineData(JobState.Active, true, true, false)]
    [InlineData(JobState.Inactive, true, true, false)]
    [InlineData(JobState.Completed, false, false, true)]
    [InlineData(JobState.Failed, false, false, true)]
    [InlineData(JobState.Cancelled, false, false, true)]
    [InlineData(JobState.Cancelling, false, false, false)]
    [InlineData(JobState.Paused, false, false, false)]
    [InlineData(JobState.Pausing, false, false, false)]
    public async Task Pause_Job(JobState state, bool intermediaryChange, bool change, bool throwsException)
    {
        var job = new JobBuilder().WithRandomInitializedState(state).Create();

        await _jobManager.CreateJobAsync(job);

        if (throwsException)
        {
            _manualResetEventSlimOuter.Set();
            await Should.ThrowAsync<JobAlreadyDoneException>(_jobStateManager.PauseJobAsync(job));
            return;
        }

        var task = Task.Run(async () => await _jobStateManager.PauseJobAsync(job));

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
                job.State.ShouldBe(JobState.Pausing);
            }
            catch
            {
                job.State.ShouldBe(JobState.Paused);
            }
        }

        _manualResetEventSlimOuter.Set();
        await task;

        job = await _jobManager.GetJobAsync(job.Id);

        if (change)
        {
            job.State.ShouldBe(JobState.Paused);
        }
        else
        {
            job.State.ShouldBe(state);
        }
    }
}
