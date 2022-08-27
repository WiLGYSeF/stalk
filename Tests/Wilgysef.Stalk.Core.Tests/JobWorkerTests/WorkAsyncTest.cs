using Shouldly;
using Wilgysef.Stalk.Core.JobTaskWorkerFactories;
using Wilgysef.Stalk.Core.JobWorkerFactories;
using Wilgysef.Stalk.Core.JobWorkerServices;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;
using Wilgysef.Stalk.Core.Tests.Extensions;
using Wilgysef.Stalk.Core.Tests.Utilities;
using Wilgysef.Stalk.TestBase;
using Wilgysef.Stalk.TestBase.Mocks;

namespace Wilgysef.Stalk.Core.Tests.JobWorkerTests;

public class WorkAsyncTest : BaseTest
{
    private readonly JobTaskWorkerFactoryMock _jobTaskWorkerFactory;
    private readonly IJobManager _jobManager;
    private readonly IJobWorkerFactory _jobWorkerFactory;

    private readonly JobWorkerStarter _jobWorkerStarter;

    public WorkAsyncTest()
    {
        _jobTaskWorkerFactory = new JobTaskWorkerFactoryMock(GetRequiredService<IServiceLocator>());

        ReplaceServiceInstance<JobTaskWorkerFactoryMock, IJobTaskWorkerFactory>(_jobTaskWorkerFactory);

        _jobManager = GetRequiredService<IJobManager>();
        _jobWorkerFactory = GetRequiredService<IJobWorkerFactory>();

        _jobWorkerStarter = new JobWorkerStarter(_jobWorkerFactory);
    }

    [Fact]
    public async Task Work_Job_Start_Tasks()
    {
        var job = new JobBuilder()
            .WithRandomInitializedState(JobState.Inactive)
            .WithRandomTasks(JobTaskState.Inactive, 3)
            .Create();
        await _jobManager.CreateJobAsync(job);

        using var workerInstance = _jobWorkerStarter.CreateAndStartWorker(job);

        job = await this.WaitUntilJobAsync(
            job.Id,
            job => job.State == JobState.Active,
            TimeSpan.FromSeconds(3));
        workerInstance.WorkerTask.Exception.ShouldBeNull();

        job.State.ShouldBe(JobState.Active);
    }

    [Fact]
    public async Task Work_Job_Wait_Tasks()
    {
        Job job;
        using (var scope = BeginLifetimeScope())
        {
            job = new JobBuilder()
                .WithRandomInitializedState(JobState.Inactive)
                .WithRandomTasks(JobTaskState.Inactive, 5)
                .Create();
            var jobManager = scope.GetRequiredService<IJobManager>();
            await jobManager.CreateJobAsync(job);
        }

        using var workerInstance = _jobWorkerStarter.CreateAndStartWorker(job);

        job = await this.WaitUntilJobAsync(
            job.Id,
            job => job.Tasks.Count(t => t.State == JobTaskState.Active) >= 4,
            TimeSpan.FromSeconds(3));
        workerInstance.WorkerTask.Exception.ShouldBeNull();

        job.Tasks.Count(t => t.State == JobTaskState.Active).ShouldBe(4);
        job.Tasks.Count(t => t.State == JobTaskState.Inactive).ShouldBe(1);
    }

    [Fact]
    public async Task Work_Job_Cycle_Tasks()
    {
        Job job;
        using (var scope = BeginLifetimeScope())
        {
            job = new JobBuilder()
                .WithRandomInitializedState(JobState.Inactive)
                .WithRandomTasks(JobTaskState.Inactive, 5)
                .Create();
            await _jobManager.CreateJobAsync(job);
        }

        using var workerInstance = _jobWorkerStarter.CreateAndStartWorker(job);

        job = await this.WaitUntilJobAsync(
            job.Id,
            job => job.Tasks.Count(t => t.State == JobTaskState.Active) >= 4,
            TimeSpan.FromSeconds(3));
        workerInstance.WorkerTask.Exception.ShouldBeNull();

        job.Tasks.Count(t => t.State == JobTaskState.Active).ShouldBe(4);

        var jobTask = job.Tasks.First(t => t.State == JobTaskState.Active);
        var nextJobTask = job.Tasks.Single(t => t.State == JobTaskState.Inactive);
        _jobTaskWorkerFactory.FinishJobTaskWorker(jobTask);

        await Task.Delay(_jobWorkerStarter.TaskWaitTimeout * 2);
        workerInstance.WorkerTask.Exception.ShouldBeNull();

        job = await this.ReloadJob(job.Id);

        var activeJobTasks = job.Tasks.Where(t => t.State == JobTaskState.Active).ToList();
        activeJobTasks.Count.ShouldBe(4);
        job.Tasks.Count(t => t.State == JobTaskState.Inactive).ShouldBe(0);
        // not done in mock
        //activeJobTasks.ShouldNotContain(t => t.Id == jobTask.Id);
        activeJobTasks.ShouldContain(t => t.Id == nextJobTask.Id);
    }

    [Fact]
    public async Task Work_Job_Finish_Tasks()
    {
        var job = new JobBuilder()
            .WithRandomInitializedState(JobState.Inactive)
            .WithRandomTasks(JobTaskState.Inactive, 1)
            .Create();
        await _jobManager.CreateJobAsync(job);

        using var workerInstance = _jobWorkerStarter.CreateAndStartWorker(job);

        job = await this.WaitUntilJobAsync(
            job.Id,
            job => job.Tasks.Any(t => t.State == JobTaskState.Active),
            TimeSpan.FromSeconds(3));
        workerInstance.WorkerTask.Exception.ShouldBeNull();

        job.Tasks.Count(t => t.State == JobTaskState.Active).ShouldBeGreaterThanOrEqualTo(1);

        var jobTask = job.Tasks.Single(t => t.State == JobTaskState.Active);
        _jobTaskWorkerFactory.FinishJobTaskWorker(jobTask);

        job = await this.WaitUntilJobAsync(
            job.Id,
            job => job.State == JobState.Completed,
            TimeSpan.FromSeconds(3));
        workerInstance.WorkerTask.Exception.ShouldBeNull();

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

        using var workerInstance = _jobWorkerStarter.CreateAndStartWorker(job);

        job = await this.WaitUntilJobAsync(
            job.Id,
            job => job.Tasks.Any(t => t.State == JobTaskState.Active),
            TimeSpan.FromSeconds(3));
        workerInstance.WorkerTask.Exception.ShouldBeNull();

        job.Tasks.Count(t => t.State == JobTaskState.Active).ShouldBeGreaterThanOrEqualTo(1);

        workerInstance.CancellationTokenSource.Cancel();

        job = await this.WaitUntilJobAsync(
            job.Id,
            job => job.State != JobState.Active,
            TimeSpan.FromSeconds(3));
        workerInstance.WorkerTask.Exception.ShouldBeNull();

        job.State.ShouldBe(JobState.Inactive);

        var jobWorkerCollectionService = GetRequiredService<IJobWorkerCollectionService>();
        jobWorkerCollectionService.Workers.ShouldBeEmpty();
    }
}
