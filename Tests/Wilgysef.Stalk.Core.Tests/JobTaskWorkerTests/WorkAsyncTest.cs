using Moq;
using Shouldly;
using Wilgysef.Stalk.Core.JobWorkerFactories;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared;
using Wilgysef.Stalk.Core.Shared.Downloaders;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Extractors;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;
using Wilgysef.Stalk.Core.Tests.Extensions;
using Wilgysef.Stalk.Core.Tests.Utilities;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Core.Tests.JobTaskWorkerTests;

public class WorkAsyncTest : BaseTest
{
    private readonly Mock<IExtractor> _extractorMock;
    private readonly Mock<IDownloader> _downloaderMock;

    private readonly IJobWorkerFactory _jobWorkerFactory;

    private readonly JobWorkerStarter _jobWorkerStarter;

    public WorkAsyncTest()
    {
        _extractorMock = new Mock<IExtractor>();
        _extractorMock.Setup(m => m.CanExtract(It.IsAny<Uri>())).Returns(true);
        _extractorMock.Setup(m => m.ExtractAsync(
            It.IsAny<Uri>(),
            It.IsAny<string>(),
            It.IsAny<IMetadataObject>(),
            It.IsAny<CancellationToken>()))
            .Returns(ExtractAsync);

        _downloaderMock = new Mock<IDownloader>();
        _downloaderMock.Setup(m => m.CanDownload(It.IsAny<Uri>())).Returns(true);
        _downloaderMock.Setup(m => m.DownloadAsync(
            It.IsAny<Uri>(),
            It.IsAny<string>(),
            It.IsAny<IMetadataObject>(),
            It.IsAny<CancellationToken>()))
                .Returns(DownloadAsync);

        ReplaceServiceInstance(_extractorMock.Object);
        ReplaceServiceInstance(_downloaderMock.Object);

        _jobWorkerFactory = GetRequiredService<IJobWorkerFactory>();

        _jobWorkerStarter = new JobWorkerStarter(_jobWorkerFactory);
    }

    [Fact]
    public async Task Work_Extract()
    {
        Job job;
        using (var scope = BeginLifetimeScope())
        {
            job = new JobBuilder()
                .WithRandomInitializedState(JobState.Inactive)
                .WithTasks(new JobTaskBuilder()
                    .WithRandomInitializedState(JobTaskState.Inactive)
                    .Create())
                .Create();
            var jobManager = scope.GetRequiredService<IJobManager>();
            await jobManager.CreateJobAsync(job);
        }
        var jobTaskId = job.Tasks.Single().Id;

        _jobWorkerStarter.EnsureTaskSuccessesOnDispose = false;
        using var workerInstance = _jobWorkerStarter.CreateAndStartWorker(job);

        job = await this.WaitUntilJobAsync(
            job.Id,
            job => job.State == JobState.Active,
            TimeSpan.FromSeconds(3));
        workerInstance.WorkerTask.Exception.ShouldBeNull();

        job.State.ShouldBe(JobState.Active);

        job = await this.WaitUntilJobAsync(
            job.Id,
            job => job.Tasks.Count >= 3,
            TimeSpan.FromSeconds(3));

        job.Tasks.Count.ShouldBeGreaterThanOrEqualTo(3);
        workerInstance.CancellationTokenSource.Cancel();

        var jobTask = job.Tasks.Single(t => t.Id == jobTaskId);
        var extractMethodInvocations = _extractorMock.Invocations.Where(i => i.Method.Name == typeof(IExtractor).GetMethod("ExtractAsync")!.Name);
        extractMethodInvocations.Any(i => (Uri)i.Arguments[0] == new Uri(jobTask.Uri)).ShouldBeTrue();

        job.Tasks.Count.ShouldBeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task Work_Download()
    {
        Job job;
        using (var scope = BeginLifetimeScope())
        {
            job = new JobBuilder()
                .WithRandomInitializedState(JobState.Inactive)
                .WithTasks(new JobTaskBuilder()
                    .WithRandomInitializedState(JobTaskState.Inactive)
                    .WithType(JobTaskType.Download)
                    .Create())
                .Create();
            var jobManager = scope.GetRequiredService<IJobManager>();
            await jobManager.CreateJobAsync(job);
        }

        using var workerInstance = _jobWorkerStarter.CreateAndStartWorker(job);

        job = await this.WaitUntilJobAsync(
            job.Id,
            job => job.State == JobState.Active,
            TimeSpan.FromSeconds(3));
        workerInstance.WorkerTask.Exception.ShouldBeNull();

        job.State.ShouldBe(JobState.Active);

        job = await this.WaitUntilJobAsync(
            job.Id,
            job => job.IsDone,
            TimeSpan.FromSeconds(3));
        workerInstance.WorkerTask.Exception.ShouldBeNull();

        job.IsDone.ShouldBeTrue();

        var jobTask = job.Tasks.Single();
        var downloadMethodInvocation = _downloaderMock.Invocations.Single(i => i.Method.Name == typeof(IDownloader).GetMethod("DownloadAsync")!.Name);
        downloadMethodInvocation.Arguments[0].ShouldBe(new Uri(jobTask.Uri));
    }

    private static async IAsyncEnumerable<DownloadResult> DownloadAsync(
        Uri uri,
        string itemData,
        IMetadataObject metadata,
        CancellationToken cancellationToken = default)
    {
        yield return new DownloadResult(
            RandomValues.RandomDirPath(3),
            new Uri(RandomValues.RandomUri()));
    }

    private static async IAsyncEnumerable<ExtractResult> ExtractAsync(
        Uri uri,
        string itemData,
        IMetadataObject metadata,
        CancellationToken cancellationToken = default)
    {
        yield return new ExtractResult(
            new Uri(RandomValues.RandomUri()),
            RandomValues.RandomString(10),
            JobTaskType.Extract);

        yield return new ExtractResult(
            new Uri(RandomValues.RandomUri()),
            RandomValues.RandomString(10),
            JobTaskType.Extract);
    }
}
