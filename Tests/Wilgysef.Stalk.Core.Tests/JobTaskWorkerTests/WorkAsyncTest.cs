using Moq;
using Shouldly;
using Wilgysef.Stalk.Core.JobWorkerFactories;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Downloaders;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Tests.Utilities;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Core.Tests.JobTaskWorkerTests;

public class WorkAsyncTest : BaseTest
{
    private readonly Mock<IDownloader> _downloaderMock;
    private readonly IJobManager _jobManager;
    private readonly IJobWorkerFactory _jobWorkerFactory;

    private readonly JobWorkerStarter _jobWorkerStarter;

    public WorkAsyncTest()
    {
        _downloaderMock = new Mock<IDownloader>();
        ReplaceServiceInstance(_downloaderMock.Object);

        _jobManager = GetRequiredService<IJobManager>();
        _jobWorkerFactory = GetRequiredService<IJobWorkerFactory>();

        _jobWorkerStarter = new JobWorkerStarter(_jobWorkerFactory);
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

        using var workerInstance = _jobWorkerStarter.CreateAndStartWorker(job);

        await WaitUntilAsync(async () =>
        {
            var job = await _jobManager.GetJobAsync(jobId);
            return job.State != JobState.Active;
        }, TimeSpan.FromSeconds(3));
        job.State.ShouldBe(JobState.Active);

        await Task.Delay(3000);

        workerInstance.WorkerTask.Exception.ShouldBeNull();
        job = await _jobManager.GetJobAsync(jobId);
        job.Tasks.Any(t => t.State == JobTaskState.Failed).ShouldBeFalse();
    }
}
