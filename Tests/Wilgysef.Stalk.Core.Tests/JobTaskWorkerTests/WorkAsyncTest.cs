﻿using Moq;
using Shouldly;
using System.Runtime.CompilerServices;
using Wilgysef.Stalk.Core.ItemIdSetServices;
using Wilgysef.Stalk.Core.JobWorkerFactories;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.CacheObjects;
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
    private readonly Mock<IItemIdSetService> _itemIdSetService;

    private readonly IJobWorkerFactory _jobWorkerFactory;

    private readonly JobWorkerStarter _jobWorkerStarter;

    public WorkAsyncTest()
    {
        _extractorMock = new Mock<IExtractor>();
        _extractorMock.Setup(m => m.Name).Returns("test");
        _extractorMock.Setup(m => m.CanExtract(It.IsAny<Uri>()))
            .Returns(true);
        _extractorMock.SetupAnyArgs<IExtractor, IAsyncEnumerable<ExtractResult>>(nameof(IExtractor.ExtractAsync))
            .Returns(ExtractAsync);

        _downloaderMock = new Mock<IDownloader>();
        _downloaderMock.Setup(m => m.Name).Returns("test");
        _downloaderMock.Setup(m => m.CanDownload(It.IsAny<Uri>()))
            .Returns(true);

        _downloaderMock.SetupAnyArgs<IDownloader, IAsyncEnumerable<DownloadResult>>(nameof(IDownloader.DownloadAsync))
            .Returns(DownloadAsync);

        _itemIdSetService = new Mock<IItemIdSetService>();
        _itemIdSetService.Setup(m => m.GetItemIdSetAsync(It.IsAny<string>()))
            .Returns(GetItemIdSetAsync);
        _itemIdSetService.Setup(m => m.WriteChangesAsync(It.IsAny<string>(), It.IsAny<IItemIdSet>()))
            .Returns(WriteChangesAsync);

        ReplaceServiceInstance(_extractorMock.Object);
        ReplaceServiceInstance(_downloaderMock.Object);
        ReplaceServiceInstance(_itemIdSetService.Object);

        _jobWorkerFactory = GetRequiredService<IJobWorkerFactory>();

        _jobWorkerStarter = new JobWorkerStarter(_jobWorkerFactory);
    }

    [Fact]
    public async Task Work_Extract()
    {
        var job = new JobBuilder()
            .WithRandomInitializedState(JobState.Inactive)
            .WithRandomTasks(JobTaskState.Inactive, 1)
            .Create();
        var jobTaskId = job.Tasks.Single().Id;

        job = await CreateRunAndCancelJob(job, 1);

        var jobTask = job.Tasks.Single(t => t.Id == jobTaskId);
        var extractMethodInvocations = _extractorMock.GetInvocations(nameof(IExtractor.ExtractAsync));
        extractMethodInvocations.Any(i => (Uri)i.Arguments[0] == new Uri(jobTask.Uri)).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Work_Download(bool testItemIds)
    {
        var builder = new JobBuilder()
            .WithRandomInitializedState(JobState.Inactive)
            .WithTasks(new JobTaskBuilder()
                .WithRandomInitializedState(JobTaskState.Inactive)
                .WithType(JobTaskType.Download)
                .Create())
            .WithConfig(new JobConfig
            {
                DownloadFilenameTemplate = "a",
            });

        if (testItemIds)
        {
            builder.Config.SaveItemIds = true;
            builder.Config.ItemIdPath = "test";
        }

        var job = await CreateRunAndCompleteJob(builder.Create());

        var jobTask = job.Tasks.Single();
        var downloadMethodInvocation = _downloaderMock.GetInvocation(nameof(IDownloader.DownloadAsync));
        downloadMethodInvocation.Arguments[0].ShouldBe(new Uri(jobTask.Uri));

        if (testItemIds)
        {
            var getItemIdSetMethodInvocation = _itemIdSetService.GetInvocation(nameof(IItemIdSetService.GetItemIdSetAsync));
            getItemIdSetMethodInvocation.Arguments[0].ShouldBe(job.GetConfig().ItemIdPath);

            var writeChangesMethodInvocation = _itemIdSetService.GetInvocation(nameof(IItemIdSetService.WriteChangesAsync));
            writeChangesMethodInvocation.Arguments[0].ShouldBe(job.GetConfig().ItemIdPath);
            (writeChangesMethodInvocation.Arguments[1] as IItemIdSet)!.Count.ShouldBe(1);
        }
    }

    [Fact]
    public async Task Reuses_JobScope_Cache()
    {
        await CreateRunAndCancelJob(
            new JobBuilder()
                .WithRandomInitializedState(JobState.Inactive)
                .WithRandomTasks(JobTaskState.Inactive, 1)
                .Create(),
            4);

        var extractMethodInvocations = _extractorMock.GetInvocations("set_Cache");
        var cache = (ICacheObject<string, object?>)extractMethodInvocations.First().Arguments[0];
        extractMethodInvocations.All(i => i.Arguments[0] == cache).ShouldBeTrue();

        await CreateRunAndCancelJob(
            new JobBuilder()
                .WithRandomInitializedState(JobState.Inactive)
                .WithRandomTasks(JobTaskState.Inactive, 1)
                .Create(),
            4);

        var otherExtractMethodInvocations = _extractorMock.GetInvocations("set_Cache")
            .Skip(extractMethodInvocations.Count);
        otherExtractMethodInvocations.First().Arguments[0].ShouldNotBe(cache);
    }

    [Fact]
    public async Task Reuses_JobScope_HttpClient()
    {
        // TODO: this may be unstable

        await CreateRunAndCancelJob(
            new JobBuilder()
                .WithRandomInitializedState(JobState.Inactive)
                .WithRandomTasks(JobTaskState.Inactive, 1)
                .Create(),
            4);

        var extractMethodInvocations = _extractorMock.GetInvocations(nameof(IExtractor.SetHttpClient));
        var client = (HttpClient)extractMethodInvocations.First().Arguments[0];
        extractMethodInvocations.All(i => i.Arguments[0] == client).ShouldBeTrue();

        await CreateRunAndCancelJob(
            new JobBuilder()
                .WithRandomInitializedState(JobState.Inactive)
                .WithRandomTasks(JobTaskState.Inactive, 1)
                .Create(),
            4);

        var otherExtractMethodInvocations = _extractorMock.GetInvocations(nameof(IExtractor.SetHttpClient))
            .Skip(extractMethodInvocations.Count);
        otherExtractMethodInvocations.First().Arguments[0].ShouldNotBe(client);
    }

    [Fact]
    public async Task Sets_Config_Extractor()
    {
        await CreateRunAndCancelJob(
            new JobBuilder()
                .WithRandomInitializedState(JobState.Inactive)
                .WithRandomTasks(JobTaskState.Inactive, 1)
                .WithConfig(new JobConfig
                {
                    ExtractorConfig = new[]
                    {
                        new JobConfig.ConfigGroup
                        {
                            Name = JobConfig.GlobalConfigGroupName,
                            Config = new Dictionary<string, object?>
                            {
                                { "a", 1 },
                            }
                        },
                        new JobConfig.ConfigGroup
                        {
                            Name = "test",
                            Config = new Dictionary<string, object?>
                            {
                                { "b", 2 },
                            }
                        }
                    }
                })
                .Create(),
            1);

        var extractMethodInvocations = _extractorMock.GetInvocations("set_Config");
        var config = (IDictionary<string, object?>)extractMethodInvocations.First().Arguments[0];
        config["a"].ShouldBe(1);
        config["b"].ShouldBe(2);
    }

    [Fact]
    public async Task Sets_Config_Downloader()
    {
        await CreateRunAndCompleteJob(
            new JobBuilder()
                .WithRandomInitializedState(JobState.Inactive)
                .WithTasks(new JobTaskBuilder()
                    .WithRandomInitializedState(JobTaskState.Inactive)
                    .WithType(JobTaskType.Download)
                    .Create())
                .WithConfig(new JobConfig
                {
                    DownloadFilenameTemplate = "a",
                    DownloaderConfig = new[]
                    {
                        new JobConfig.ConfigGroup
                        {
                            Name = JobConfig.GlobalConfigGroupName,
                            Config = new Dictionary<string, object?>
                            {
                                { "a", 1 },
                            }
                        },
                        new JobConfig.ConfigGroup
                        {
                            Name = "test",
                            Config = new Dictionary<string, object?>
                            {
                                { "b", 2 },
                            }
                        }
                    }
                })
                .Create());

        var downloadMethodInvocations = _downloaderMock.GetInvocations("set_Config");
        var config = (IDictionary<string, object?>)downloadMethodInvocations.First().Arguments[0];
        config["a"].ShouldBe(1);
        config["b"].ShouldBe(2);
    }

    private async Task<(Job Job, JobWorkerStarter.JobWorkerInstance WorkerInstance)> CreateAndRunJob(Job job)
    {
        using (var scope = BeginLifetimeScope())
        {
            var jobManager = scope.GetRequiredService<IJobManager>();
            await jobManager.CreateJobAsync(job);
        }

        _jobWorkerStarter.EnsureTaskSuccessesOnDispose = false;
        var workerInstance = _jobWorkerStarter.CreateAndStartWorker(job);

        job = await this.WaitUntilJobAsync(
            job.Id,
            job => job.State == JobState.Active,
            TimeSpan.FromSeconds(3));
        workerInstance.WorkerTask.Exception.ShouldBeNull();

        job.State.ShouldBe(JobState.Active);

        return (job, workerInstance);
    }

    private async Task<Job> CreateRunAndCompleteJob(Job job)
    {
        var initialTaskCount = job.Tasks.Count;
        (job, var workerInstance) = await CreateAndRunJob(job);

        job = await this.WaitUntilJobAsync(
            job.Id,
            job => job.IsDone,
            TimeSpan.FromSeconds(3));

        job.IsDone.ShouldBeTrue();
        workerInstance.Dispose();

        return job;
    }

    private async Task<Job> CreateRunAndCancelJob(Job job, int minimumTasksAdded)
    {
        // TODO: this may be unstable

        var initialTaskCount = job.Tasks.Count;
        (job, var workerInstance) = await CreateAndRunJob(job);

        job = await this.WaitUntilJobAsync(
            job.Id,
            job => job.Tasks.Count >= initialTaskCount + minimumTasksAdded,
            TimeSpan.FromSeconds(3));

        job.Tasks.Count.ShouldBeGreaterThanOrEqualTo(initialTaskCount + minimumTasksAdded);
        workerInstance.CancellationTokenSource.Cancel();

        job = await this.WaitUntilJobAsync(
            job.Id,
            job => !job.IsActive,
            TimeSpan.FromSeconds(3));

        job.IsActive.ShouldBeFalse();
        workerInstance.Dispose();

        return job;
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private static async IAsyncEnumerable<ExtractResult> ExtractAsync(
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        Uri uri,
        string itemData,
        IMetadataObject metadata,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
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

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private static async IAsyncEnumerable<DownloadResult> DownloadAsync(
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        Uri uri,
        string filenameTemplate,
        string itemId,
        string itemData,
        string metadataFilenameTemplate,
        IMetadataObject metadata,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new DownloadResult(
            RandomValues.RandomDirPath(3),
            new Uri(RandomValues.RandomUri()),
            RandomValues.RandomString(10));
    }

    private Task<IItemIdSet> GetItemIdSetAsync(string path)
    {
        return Task.FromResult((IItemIdSet)new ItemIdSet());
    }

    private Task<int> WriteChangesAsync(string path, IItemIdSet itemIds)
    {
        return Task.FromResult(itemIds.PendingItems.Count);
    }
}
