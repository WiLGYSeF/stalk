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
    private readonly JobTaskWorkerFactoryMock _jobTaskWorkerFactory;
    private readonly IJobManager _jobManager;
    private readonly IJobWorkerFactory _jobWorkerFactory;
    private Task _jobWorkerTask;

    public WorkAsyncTest()
    {
        _jobTaskWorkerFactory = new JobTaskWorkerFactoryMock(GetRequiredService<IServiceLocator>());

        ReplaceService<DefaultDownloaderMock, IDownloader>();
        //ReplaceServiceInstance<JobTaskWorkerFactoryMock, IJobTaskWorkerFactory>(_jobTaskWorkerFactory);

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
        await _jobManager.CreateJobAsync(job);

        CreateAndStartWorker(job, out _);

        WaitUntil(() => job.State == JobState.Active, TimeSpan.FromSeconds(3));
        job.State.ShouldBe(JobState.Active);

        await Task.Delay(TimeSpan.FromSeconds(3));
        _jobWorkerTask!.Exception.ShouldBeNull();
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
