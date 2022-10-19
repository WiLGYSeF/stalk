using Moq;
using Shouldly;
using System.Net;
using Wilgysef.MoqExtensions;
using Wilgysef.Stalk.Core.JobWorkerFactories;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Downloaders;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Extractors;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;
using Wilgysef.Stalk.Core.Tests.Utilities;
using Wilgysef.Stalk.TestBase;
using Wilgysef.Stalk.TestBase.Extensions;

namespace Wilgysef.Stalk.Core.Tests.JobTaskWorkerTests;

public class TaskDelayTooManyRequestsTest : BaseTest
{
    private readonly Mock<IExtractor> _extractorMock;
    private readonly Mock<IDownloader> _downloaderMock;

    private readonly IJobWorkerFactory _jobWorkerFactory;

    private readonly JobWorkerStarter _jobWorkerStarter;

    public TaskDelayTooManyRequestsTest()
    {
        _extractorMock = new Mock<IExtractor>();
        _extractorMock.Setup(m => m.CanExtract(It.IsAny<Uri>()))
            .Returns(true);
        _extractorMock.SetupAnyArgs<IExtractor, IAsyncEnumerable<ExtractResult>>(nameof(IExtractor.ExtractAsync))
            .Returns(ExtractAsync);

        _downloaderMock = new Mock<IDownloader>();
        _downloaderMock.Setup(m => m.CanDownload(It.IsAny<Uri>()))
            .Returns(true);
        _downloaderMock.SetupAnyArgs<IDownloader, IAsyncEnumerable<DownloadResult>>(nameof(IDownloader.DownloadAsync))
            .Returns(DownloadAsync);

        ReplaceServiceInstance(_extractorMock.Object);
        ReplaceServiceInstance(_downloaderMock.Object);

        _jobWorkerFactory = GetRequiredService<IJobWorkerFactory>();

        _jobWorkerStarter = new JobWorkerStarter(_jobWorkerFactory);
    }

    [Fact]
    public async Task Retry_Task()
    {
        Job job;
        using (var scope = BeginLifetimeScope())
        {
            job = new JobBuilder()
                .WithRandomInitializedState(JobState.Inactive)
                .WithRandomTasks(JobTaskState.Inactive, 1)
                .WithConfig(new JobConfig
                {
                    Delay = new JobConfig.DelayConfig
                    {
                        TooManyRequestsDelay = new JobConfig.Range(100, 100)
                    }
                })
                .Create();
            var jobManager = scope.GetRequiredService<IJobManager>();
            await jobManager.CreateJobAsync(job);
        }
        var jobTaskId = job.Tasks.Single().Id;

        _jobWorkerStarter.EnsureTaskSuccessesOnDispose = false;
        using var workerInstance = _jobWorkerStarter.CreateAndStartWorker(job);

        job = await this.WaitUntilJobAsync(job.Id, job => job.Tasks.Count >= 2);

        workerInstance.CancellationTokenSource.Cancel();

        var jobTask = job.Tasks.Single(t => t.Id == jobTaskId);
        var retryTask = job.Tasks.First(t => t.Id != jobTaskId);
        retryTask.Uri.ShouldBe(jobTask.Uri);
        (retryTask.DelayedUntil!.Value - DateTime.Now).TotalSeconds.ShouldBeInRange(90, 100);
    }

    [Fact]
    public async Task Retry_Task_NoDelay()
    {
        Job job;
        using (var scope = BeginLifetimeScope())
        {
            job = new JobBuilder()
                .WithRandomInitializedState(JobState.Inactive)
                .WithRandomTasks(JobTaskState.Inactive, 1)
                .Create();
            var jobManager = scope.GetRequiredService<IJobManager>();
            await jobManager.CreateJobAsync(job);
        }
        var jobTaskId = job.Tasks.Single().Id;

        _jobWorkerStarter.EnsureTaskSuccessesOnDispose = false;
        using var workerInstance = _jobWorkerStarter.CreateAndStartWorker(job);

        job = await this.WaitUntilJobAsync(job.Id, job => job.Tasks.Count >= 2);

        workerInstance.CancellationTokenSource.Cancel();

        var jobTask = job.Tasks.Single(t => t.Id == jobTaskId);
        var retryTask = job.Tasks.First(t => t.Id != jobTaskId);
        retryTask.Uri.ShouldBe(jobTask.Uri);
        retryTask.DelayedUntil.ShouldBeNull();
    }

    private static IAsyncEnumerable<ExtractResult> ExtractAsync(
        Uri uri,
        string? itemData,
        IMetadataObject metadata,
        CancellationToken cancellationToken = default)
    {
        throw new HttpRequestException("Mock download exception.", null, HttpStatusCode.TooManyRequests);
    }

    private static IAsyncEnumerable<DownloadResult> DownloadAsync(
        Uri uri,
        string filenameTemplate,
        string? itemId,
        string? metadataFilenameTemplate,
        IMetadataObject metadata,
        DownloadRequestData? requestData = null,
        CancellationToken cancellationToken = default)
    {
        throw new HttpRequestException("Mock download exception.", null, HttpStatusCode.TooManyRequests);
    }
}
