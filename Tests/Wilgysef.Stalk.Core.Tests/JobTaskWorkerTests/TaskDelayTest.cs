using Moq;
using Shouldly;
using System.Net;
using System.Runtime.CompilerServices;
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

public class TaskDelayTest : BaseTest
{
    private bool ThrowExtractException { get; set; } = false;
    private bool ThrowDownloadException { get; set; } = false;

    private readonly Mock<IExtractor> _extractorMock;
    private readonly Mock<IDownloader> _downloaderMock;

    private readonly IJobWorkerFactory _jobWorkerFactory;

    private readonly JobWorkerStarter _jobWorkerStarter;

    public TaskDelayTest()
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
    public async Task Retry_JobTask()
    {
        ThrowExtractException = true;

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
                        TaskFailedDelay = new JobConfig.Range(100, 100)
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
        job = await this.WaitUntilJobAsync(job.Id, job => job.Tasks.Single(t => t.Id == jobTaskId).IsDone);

        var jobTask = job.Tasks.Single(t => t.Id == jobTaskId);
        var retryTask = job.Tasks.First(t => t.Id != jobTaskId);

        jobTask.Result.RetryJobTaskId.ShouldBe(retryTask.Id);
        retryTask.Uri.ShouldBe(jobTask.Uri);
        (retryTask.DelayedUntil!.Value - DateTime.Now).TotalSeconds.ShouldBeInRange(90, 100);
    }

    [Fact]
    public async Task Retry_JobTask_NoDelay()
    {
        // TODO: unstable test

        ThrowExtractException = true;

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
        job = await this.WaitUntilJobAsync(job.Id, job => job.Tasks.Single(t => t.Id == jobTaskId).IsDone);

        var jobTask = job.Tasks.Single(t => t.Id == jobTaskId);
        var retryTask = job.Tasks.First(t => t.Id != jobTaskId);

        jobTask.Result.RetryJobTaskId.ShouldBe(retryTask.Id);
        retryTask.Uri.ShouldBe(jobTask.Uri);
        retryTask.DelayedUntil.ShouldBeNull();
    }

    [Fact]
    public async Task Delay_JobTask()
    {
        // TODO: unstable test

        ThrowExtractException = false;

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
                        TaskDelay = new JobConfig.Range(100, 100)
                    }
                })
                .Create();
            var jobManager = scope.GetRequiredService<IJobManager>();
            await jobManager.CreateJobAsync(job);
        }
        var jobTaskId = job.Tasks.Single().Id;

        using var workerInstance = _jobWorkerStarter.CreateAndStartWorker(job);

        job = await this.WaitUntilJobAsync(job.Id, job => job.Tasks.Count >= 2);

        workerInstance.CancellationTokenSource.Cancel();
        job = await this.WaitUntilJobAsync(job.Id, job => job.Tasks.Single(t => t.Id == jobTaskId).IsDone);

        var jobTask = job.Tasks.Single(t => t.Id == jobTaskId);
        var retryTask = job.Tasks.First(t => t.Id != jobTaskId);
        (retryTask.DelayedUntil!.Value - DateTime.Now).TotalSeconds.ShouldBeInRange(90, 100);
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private async IAsyncEnumerable<ExtractResult> ExtractAsync(
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        Uri uri,
        string itemData,
        IMetadataObject metadata,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (ThrowExtractException)
        {
            throw new HttpRequestException("Mock download exception.", null, HttpStatusCode.InternalServerError);
        }
        yield return new ExtractResult(
            RandomValues.RandomUri(),
            RandomValues.RandomString(10),
            JobTaskType.Download);
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private async IAsyncEnumerable<DownloadResult> DownloadAsync(
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        Uri uri,
        string filenameTemplate,
        string itemId,
        string metadataFilenameTemplate,
        IMetadataObject metadata,
        DownloadRequestData? requestData = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (ThrowDownloadException)
        {
            throw new HttpRequestException("Mock download exception.", null, HttpStatusCode.InternalServerError);
        }
        yield return new DownloadResult(
            RandomValues.RandomFilePath(2),
            RandomValues.RandomUri(),
            null);
    }
}
