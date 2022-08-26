using Shouldly;
using Wilgysef.Stalk.Core.JobTaskWorkerFactories;
using Wilgysef.Stalk.Core.JobWorkerFactories;
using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.JobWorkerServices;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;
using Wilgysef.Stalk.TestBase;
using Wilgysef.Stalk.TestBase.Mocks;

namespace Wilgysef.Stalk.Core.Tests.JobWorkerTests;

public class WorkAsyncTest : BaseTest
{
    private readonly JobTaskWorkerFactoryMock _jobTaskWorkerFactory;
    private readonly IJobManager _jobManager;
    private readonly IJobWorkerFactory _jobWorkerFactory;
    private Task? _jobWorkerTask;

    public WorkAsyncTest()
    {
        _jobTaskWorkerFactory = new JobTaskWorkerFactoryMock(GetRequiredService<IServiceLocator>());

        ReplaceServiceInstance<JobTaskWorkerFactoryMock, IJobTaskWorkerFactory>(_jobTaskWorkerFactory);

        _jobManager = GetRequiredService<IJobManager>();
        _jobWorkerFactory = GetRequiredService<IJobWorkerFactory>();
    }

    [Fact]
    public async Task Work_Job_Start_Tasks()
    {
        var job = new JobBuilder()
            .WithRandomInitializedState(JobState.Inactive)
            .WithRandomTasks(JobTaskState.Inactive, 3)
            .Create();
        await _jobManager.CreateJobAsync(job);

        CreateAndStartWorker(job, out _);

        WaitUntil(() => job.State == JobState.Active, TimeSpan.FromSeconds(3));
        _jobWorkerTask!.Exception.ShouldBeNull();

        job.State.ShouldBe(JobState.Active);
    }

    [Fact]
    public async Task Work_Job_Wait_Tasks()
    {
        var job = new JobBuilder()
            .WithRandomInitializedState(JobState.Inactive)
            .WithRandomTasks(JobTaskState.Inactive, 5)
            .Create();
        var jobId = job.Id;
        await _jobManager.CreateJobAsync(job);

        CreateAndStartWorker(job, out _);

        await WaitUntilAsync(async () =>
        {
            //job = await _jobManager.GetJobAsync(jobId);
            return job.Tasks.Count(t => t.State == JobTaskState.Active) >= 4;
        }, TimeSpan.FromSeconds(3));
        _jobWorkerTask!.Exception.ShouldBeNull();

        job = await _jobManager.GetJobAsync(jobId);
        job.Tasks.Count(t => t.State == JobTaskState.Active).ShouldBe(4);
        job.Tasks.Count(t => t.State == JobTaskState.Inactive).ShouldBe(1);

        await Task.Delay(1100);
        _jobWorkerTask!.Exception.ShouldBeNull();

        job.Tasks.Count(t => t.State == JobTaskState.Active).ShouldBe(4);
        job.Tasks.Count(t => t.State == JobTaskState.Inactive).ShouldBe(1);
    }

    [Fact]
    public async Task Work_Job_Cycle_Tasks()
    {
        var job = new JobBuilder()
            .WithRandomInitializedState(JobState.Inactive)
            .WithRandomTasks(JobTaskState.Inactive, 5)
            .Create();
        await _jobManager.CreateJobAsync(job);

        CreateAndStartWorker(job, out _);

        WaitUntil(() => job.Tasks.Count(t => t.State == JobTaskState.Active) >= 4, TimeSpan.FromSeconds(3));
        _jobWorkerTask!.Exception.ShouldBeNull();

        var jobTask = job.Tasks.First(t => t.State == JobTaskState.Active);
        var nextJobTask = job.Tasks.Single(t => t.State == JobTaskState.Inactive);
        _jobTaskWorkerFactory.FinishJobTaskWorker(jobTask);

        await Task.Delay(1100);
        _jobWorkerTask!.Exception.ShouldBeNull();

        var activeJobTasks = job.Tasks.Where(t => t.State == JobTaskState.Active).ToList();
        job.Tasks.Count(t => t.State == JobTaskState.Inactive).ShouldBe(0);
        // not done in mock
        //activeJobTasks.ShouldNotContain(t => t.Id != jobTask.Id);
        activeJobTasks.ShouldContain(t => t.Id != jobTask.Id);
    }

    [Fact]
    public async Task Work_Job_Finish_Tasks()
    {
        var job = new JobBuilder()
            .WithRandomInitializedState(JobState.Inactive)
            .WithRandomTasks(JobTaskState.Inactive, 1)
            .Create();
        await _jobManager.CreateJobAsync(job);

        CreateAndStartWorker(job, out _);

        WaitUntil(() => job.Tasks.Count(t => t.State == JobTaskState.Active) >= 1, TimeSpan.FromSeconds(3));
        job.Tasks.Count(t => t.State == JobTaskState.Active).ShouldBeGreaterThanOrEqualTo(1);

        var jobTask = job.Tasks.Single(t => t.State == JobTaskState.Active);
        _jobTaskWorkerFactory.FinishJobTaskWorker(jobTask);

        WaitUntil(() => job.State == JobState.Completed, TimeSpan.FromSeconds(3));
        _jobWorkerTask!.Exception.ShouldBeNull();

        job.State.ShouldBe(JobState.Completed);

        var jobWorkerCollectionService = GetRequiredService<IJobWorkerCollectionService>();
        jobWorkerCollectionService.Workers.ShouldBeEmpty();
    }

    [Fact]
    public async Task Work_Job_Cancel()
    {
        var job = new JobBuilder()
            .WithRandomInitializedState(JobState.Inactive)
            .WithRandomTasks(JobTaskState.Inactive, 1)
            .Create();
        await _jobManager.CreateJobAsync(job);

        CreateAndStartWorker(job, out var cancellationTokenSource);

        WaitUntil(() => job.Tasks.Count(t => t.State == JobTaskState.Active) >= 1, TimeSpan.FromSeconds(3));
        job.Tasks.Count(t => t.State == JobTaskState.Active).ShouldBeGreaterThanOrEqualTo(1);

        cancellationTokenSource.Cancel();

        WaitUntil(() => job.State != JobState.Active, TimeSpan.FromSeconds(3));
        _jobWorkerTask!.Exception.ShouldBeNull();

        job.State.ShouldBe(JobState.Inactive);

        var jobWorkerCollectionService = GetRequiredService<IJobWorkerCollectionService>();
        jobWorkerCollectionService.Workers.ShouldBeEmpty();
    }

    private IJobWorker CreateAndStartWorker(Job job, out CancellationTokenSource cancellationTokenSource)
    {
        var worker = _jobWorkerFactory.CreateWorker(job);
        worker.TaskWaitTimeoutMilliseconds = 100;

        var cts = new CancellationTokenSource();
        cancellationTokenSource = cts;
        _jobWorkerTask = Task.Run(async () => await worker.WorkAsync(cts.Token));
        return worker;
    }
}
