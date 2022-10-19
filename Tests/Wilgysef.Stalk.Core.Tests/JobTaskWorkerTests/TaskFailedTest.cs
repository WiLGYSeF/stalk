using Moq;
using Shouldly;
using System.Runtime.CompilerServices;
using Wilgysef.MoqExtensions;
using Wilgysef.Stalk.Core.JobWorkerFactories;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Downloaders;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.Exceptions;
using Wilgysef.Stalk.Core.Shared.Extractors;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;
using Wilgysef.Stalk.Core.Tests.Utilities;
using Wilgysef.Stalk.TestBase;
using Wilgysef.Stalk.TestBase.Extensions;

namespace Wilgysef.Stalk.Core.Tests.JobTaskWorkerTests;

public class TaskFailedTest : BaseTest
{
    private Exception? ExtractException { get; set; }
    private Exception? DownloadException { get; set; }

    private int? ExtractExceptionCount { get; set; }
    private int? DownloadExceptionCount { get; set; }

    private int _extractExceptions = 0;
    private int _downloadExceptions = 0;

    private readonly object _extractLock = new();
    private readonly object _downloadLock = new();

    private readonly Mock<IExtractor> _extractorMock;
    private readonly Mock<IDownloader> _downloaderMock;

    private readonly IJobWorkerFactory _jobWorkerFactory;

    private readonly JobWorkerStarter _jobWorkerStarter;

    public TaskFailedTest()
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
    public async Task Task_Threw_JobTaskWorkerException_NoRetry()
    {
        ExtractException = new JobTaskWorkerException("a");

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

        job = await this.WaitUntilJobAsync(job.Id, job => job.IsDone);

        var jobTask = job.Tasks.Single();
        jobTask.Result.RetryJobTaskId.ShouldBeNull();
    }

    [Fact]
    public async Task Job_Failed_With_FailedTasks_NoRetry()
    {
        // TODO: unstable test

        DownloadException = new JobTaskWorkerException("a");
        DownloadExceptionCount = 1;

        Job job;
        using (var scope = BeginLifetimeScope())
        {
            job = new JobBuilder()
                .WithRandomInitializedState(JobState.Inactive)
                .WithRandomTasks(JobTaskState.Inactive, 3, JobTaskType.Download)
                .WithConfig(new JobConfig
                {
                    DownloadFilenameTemplate = "a"
                })
                .Create();
            var jobManager = scope.GetRequiredService<IJobManager>();
            await jobManager.CreateJobAsync(job);
        }

        _jobWorkerStarter.EnsureTaskSuccessesOnDispose = false;
        using var workerInstance = _jobWorkerStarter.CreateAndStartWorker(job);

        job = await this.WaitUntilJobAsync(job.Id, job => job.IsDone);
        job.Tasks.Count.ShouldBe(3);
        job.State.ShouldBe(JobState.Failed);
        job.Tasks.Count(t => t.State == JobTaskState.Completed).ShouldBe(2);
        job.Tasks.Count(t => t.State == JobTaskState.Failed).ShouldBe(1);
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private async IAsyncEnumerable<ExtractResult> ExtractAsync(
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        Uri uri,
        string itemData,
        IMetadataObject metadata,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        lock (_extractLock)
        {
            if (ExtractException != null && (!ExtractExceptionCount.HasValue || _extractExceptions < ExtractExceptionCount.Value))
            {
                _extractExceptions++;
                throw ExtractException;
            }
            yield return new ExtractResult(
                RandomValues.RandomUri(),
                RandomValues.RandomString(10),
                JobTaskType.Download);
        }
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
        lock (_downloadLock)
        {
            if (DownloadException != null && (!DownloadExceptionCount.HasValue || _downloadExceptions < DownloadExceptionCount.Value))
            {
                _downloadExceptions++;
                throw DownloadException;
            }
            yield return new DownloadResult(
                RandomValues.RandomFilePath(2),
                RandomValues.RandomUri(),
                null);
        }
    }
}
