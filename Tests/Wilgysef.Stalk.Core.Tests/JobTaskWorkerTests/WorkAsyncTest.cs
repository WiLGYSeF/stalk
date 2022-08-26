using Moq;
using Shouldly;
using Wilgysef.Stalk.Core.JobTaskWorkerFactories;
using Wilgysef.Stalk.Core.JobWorkerFactories;
using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Downloaders;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;
using Wilgysef.Stalk.TestBase;
using Wilgysef.Stalk.TestBase.Mocks;

namespace Wilgysef.Stalk.Core.Tests.JobTaskWorkerTests;

public class WorkAsyncTest : BaseTest
{
    private readonly Mock<IDownloader> _downloaderMock;
    private readonly IJobManager _jobManager;
    private readonly IJobWorkerFactory _jobWorkerFactory;
    private Task _jobWorkerTask;

    public WorkAsyncTest()
    {
        _downloaderMock = new Mock<IDownloader>();
        ReplaceServiceInstance(_downloaderMock.Object);

        _jobManager = GetRequiredService<IJobManager>();
        _jobWorkerFactory = GetRequiredService<IJobWorkerFactory>();
    }

    [Fact]
    public async Task Work_Extract()
    {

    }

    [Fact]
    public async Task Work_Download()
    {
        var job = new JobBuilder()
            .WithRandomInitializedState(JobState.Inactive)
            .WithTasks(new JobTaskBuilder()
                .WithRandomInitializedState(JobTaskState.Inactive)
                .WithType(JobTaskType.Download)
                .Create())
            .Create();
        var jobId = job.Id;
        await _jobManager.CreateJobAsync(job);

        CreateAndStartWorker(job, out _);

        await WaitUntilAsync(async () =>
        {
            var job = await _jobManager.GetJobAsync(jobId);
            return job.State != JobState.Active;
        }, TimeSpan.FromSeconds(3));
        job.State.ShouldBe(JobState.Active);

        await Task.Delay(3000);

        _jobWorkerTask!.Exception.ShouldBeNull();
        job = await _jobManager.GetJobAsync(jobId);
        job.Tasks.Any(t => t.State == JobTaskState.Failed).ShouldBeFalse();
    }

    // TODO: duplicate code
    private IJobWorker CreateAndStartWorker(Job job, out CancellationTokenSource cancellationTokenSource)
    {
        var worker = _jobWorkerFactory.CreateWorker(job);
        var cts = new CancellationTokenSource();
        cancellationTokenSource = cts;
        _jobWorkerTask = Task.Run(async () => await worker.WorkAsync(cts.Token));
        return worker;
    }
}
