using Moq;
using Shouldly;
using Wilgysef.Stalk.Core.ItemIdSetServices;
using Wilgysef.Stalk.Core.JobWorkerFactories;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;
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
        _extractorMock.Setup(m => m.CanExtract(It.IsAny<Uri>()))
            .Returns(true);
        _extractorMock.Setup(m => m.ExtractAsync(
            It.IsAny<Uri>(),
            It.IsAny<string>(),
            It.IsAny<IMetadataObject>(),
            It.IsAny<CancellationToken>()))
            .Returns(ExtractAsync);

        _downloaderMock = new Mock<IDownloader>();
        _downloaderMock.Setup(m => m.CanDownload(It.IsAny<Uri>()))
            .Returns(true);
        _downloaderMock.Setup(m => m.DownloadAsync(
            It.IsAny<Uri>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<IMetadataObject>(),
            It.IsAny<CancellationToken>()))
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
        var extractMethodInvocations = _extractorMock.GetInvocations("ExtractAsync");
        extractMethodInvocations.Any(i => (Uri)i.Arguments[0] == new Uri(jobTask.Uri)).ShouldBeTrue();

        job.Tasks.Count.ShouldBeGreaterThanOrEqualTo(3);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Work_Download(bool testItemIds)
    {
        Job job;
        using (var scope = BeginLifetimeScope())
        {
            var builder = new JobBuilder()
                .WithRandomInitializedState(JobState.Inactive)
                .WithTasks(new JobTaskBuilder()
                    .WithRandomInitializedState(JobTaskState.Inactive)
                    .WithType(JobTaskType.Download)
                    .Create());

            if (testItemIds)
            {
                builder.Config.SaveItemIds = true;
                builder.Config.ItemIdPath = "test";
            }

            job = builder.Create();
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
        var downloadMethodInvocation = _downloaderMock.GetInvocation("DownloadAsync");
        downloadMethodInvocation.Arguments[0].ShouldBe(new Uri(jobTask.Uri));

        if (testItemIds)
        {
            var getItemIdSetMethodInvocation = _itemIdSetService.GetInvocation("GetItemIdSetAsync");
            getItemIdSetMethodInvocation.Arguments[0].ShouldBe(job.GetConfig().ItemIdPath);

            var writeChangesMethodInvocation = _itemIdSetService.GetInvocation("WriteChangesAsync");
            writeChangesMethodInvocation.Arguments[0].ShouldBe(job.GetConfig().ItemIdPath);
            (writeChangesMethodInvocation.Arguments[1] as IItemIdSet)!.Count.ShouldBe(1);
        }
    }

    private static async IAsyncEnumerable<DownloadResult> DownloadAsync(
        Uri uri,
        string filenameTemplate,
        string itemId,
        string itemData,
        string metadataFilenameTemplate,
        IMetadataObject metadata,
        CancellationToken cancellationToken = default)
    {
        yield return new DownloadResult(
            RandomValues.RandomDirPath(3),
            new Uri(RandomValues.RandomUri()),
            RandomValues.RandomString(10));
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

    private Task<IItemIdSet> GetItemIdSetAsync(string path)
    {
        return Task.FromResult((IItemIdSet)new ItemIdSet());
    }

    private Task<int> WriteChangesAsync(string path, IItemIdSet itemIds)
    {
        return Task.FromResult(itemIds.PendingItems.Count);
    }
}
