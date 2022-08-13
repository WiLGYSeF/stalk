using Moq;
using Shouldly;
using Wilgysef.Stalk.Core.JobWorkerManagers;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Exceptions;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Core.Tests.JobStateManagerTests;

public class StopJobTaskAsyncTest : BaseTest
{
    private readonly IJobManager _jobManager;
    private readonly IJobStateManager _jobStateManager;

    private readonly ManualResetEventSlim _manualResetEventSlimOuter = new();
    private readonly ManualResetEventSlim _manualResetEventSlimInner = new();

    public StopJobTaskAsyncTest()
    {
        var jobTaskWorkerService = new Mock<IJobTaskWorkerService>();

        jobTaskWorkerService.Setup(s => s.StopJobTaskWorker(It.IsAny<JobTask>())).Callback(() =>
        {
            _manualResetEventSlimInner.Set();
            _manualResetEventSlimOuter.Wait();
        });

        ReplaceServiceInstance<IJobTaskWorkerService, IJobTaskWorkerService>(jobTaskWorkerService.Object);

        _jobManager = GetRequiredService<IJobManager>();
        _jobStateManager = GetRequiredService<IJobStateManager>();
    }

    [Theory]
    [InlineData(JobTaskState.Active, true, true)]
    [InlineData(JobTaskState.Inactive, true, true)]
    [InlineData(JobTaskState.Completed, false, false)]
    [InlineData(JobTaskState.Failed, false, false)]
    [InlineData(JobTaskState.Cancelled, false, false)]
    [InlineData(JobTaskState.Cancelling, false, false)]
    [InlineData(JobTaskState.Paused, false, false)]
    [InlineData(JobTaskState.Pausing, false, false)]
    public async Task Stop_Job(JobTaskState state, bool intermediaryChange, bool change)
    {
        var job = new JobBuilder().WithRandomInitializedState(JobState.Active)
            .WithRandomTasks(state, 1)
            .Create();
        var jobTask = job.Tasks.First();

        await _jobManager.CreateJobAsync(job);

        var task = Task.Run(async () => await _jobStateManager.StopJobTaskAsync(job, jobTask));

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
            jobTask = job.Tasks.First();

            try
            {
                jobTask.State.ShouldBe(JobTaskState.Cancelling);
            }
            catch
            {
                jobTask.State.ShouldBe(JobTaskState.Cancelled);
            }
        }

        _manualResetEventSlimOuter.Set();
        await task;

        job = await _jobManager.GetJobAsync(job.Id);
        jobTask = job.Tasks.First();

        if (change)
        {
            jobTask.State.ShouldBe(JobTaskState.Cancelled);
        }
        else
        {
            if (state == JobTaskState.Paused)
            {
                jobTask.State.ShouldBe(JobTaskState.Cancelled);
            }
            else
            {
                jobTask.State.ShouldBe(state);
            }
        }
    }
}
