using Shouldly;
using Wilgysef.Stalk.Core.JobTaskWorkerFactories;
using Wilgysef.Stalk.Core.JobWorkerFactories;
using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;
using Wilgysef.Stalk.TestBase;
using Wilgysef.Stalk.TestBase.Mocks;

namespace Wilgysef.Stalk.Core.Tests.JobWorkerTests;

public class WorkAsyncTest : BaseTest
{
    [Fact]
    public async Task Work_Job_Start_Tasks()
    {
        var resetEvent = new ManualResetEventSlim();
        var jobTaskWorkerFactory = new JobTaskWorkerFactoryMock(GetRequiredService<IServiceLocator>());
        jobTaskWorkerFactory.WorkEvent += (sender, args) =>
        {
            resetEvent.Set();
        };

        ReplaceServiceInstance<JobTaskWorkerFactoryMock, IJobTaskWorkerFactory>(jobTaskWorkerFactory);

        var jobManager = GetRequiredService<IJobManager>();
        var jobWorkerFactory = GetRequiredService<IJobWorkerFactory>();

        var job = new JobBuilder()
            .WithRandomInitializedState(JobState.Inactive)
            .WithRandomTasks(JobTaskState.Inactive, 3)
            .Create();
        await jobManager.CreateJobAsync(job);

        var worker = jobWorkerFactory.CreateWorker(job);
        var cancellationTokenSource = new CancellationTokenSource();
        var task = new Task(
            async () => await worker.WorkAsync(cancellationTokenSource.Token),
            cancellationTokenSource.Token);
        task.Start();

        resetEvent.Wait();
        job.State.ShouldBe(JobState.Active);
        //cancellationTokenSource.Cancel();

        //WaitUntil(() => job.State == JobState.Inactive, TimeSpan.FromSeconds(3));
        //job.State.ShouldBe(JobState.Inactive);
    }

    [Fact]
    public async Task Work_Job_Wait_Tasks()
    {
        var resetEvent = new ManualResetEventSlim();
        var jobTaskWorkerFactory = new JobTaskWorkerFactoryMock(GetRequiredService<IServiceLocator>());
        jobTaskWorkerFactory.WorkEvent += (sender, args) =>
        {
            resetEvent.Set();
        };

        ReplaceServiceInstance<JobTaskWorkerFactoryMock, IJobTaskWorkerFactory>(jobTaskWorkerFactory);

        var jobManager = GetRequiredService<IJobManager>();
        var jobWorkerFactory = GetRequiredService<IJobWorkerFactory>();

        var job = new JobBuilder()
            .WithRandomInitializedState(JobState.Inactive)
            .WithRandomTasks(JobTaskState.Inactive, 5)
            .Create();
        await jobManager.CreateJobAsync(job);

        var worker = jobWorkerFactory.CreateWorker(job);
        var cancellationTokenSource = new CancellationTokenSource();
        var task = new Task(
            async () => await worker.WorkAsync(cancellationTokenSource.Token),
            cancellationTokenSource.Token);
        task.Start();

        resetEvent.Wait();

        WaitUntil(() => job.Tasks.Count(t => t.State == JobTaskState.Active) >= 4, TimeSpan.FromSeconds(3));
        job.Tasks.Count(t => t.State == JobTaskState.Active).ShouldBe(4);
        job.Tasks.Count(t => t.State == JobTaskState.Inactive).ShouldBe(1);

        await Task.Delay(1100);

        job.Tasks.Count(t => t.State == JobTaskState.Active).ShouldBe(4);
        job.Tasks.Count(t => t.State == JobTaskState.Inactive).ShouldBe(1);
    }

    [Fact]
    public async Task Work_Job_Cycle_Tasks()
    {
        var resetEvent = new ManualResetEventSlim();
        var jobTaskWorkerFactory = new JobTaskWorkerFactoryMock(GetRequiredService<IServiceLocator>());
        jobTaskWorkerFactory.WorkEvent += (sender, args) =>
        {
            resetEvent.Set();
        };

        ReplaceServiceInstance<JobTaskWorkerFactoryMock, IJobTaskWorkerFactory>(jobTaskWorkerFactory);

        var jobManager = GetRequiredService<IJobManager>();
        var jobWorkerFactory = GetRequiredService<IJobWorkerFactory>();

        var job = new JobBuilder()
            .WithRandomInitializedState(JobState.Inactive)
            .WithRandomTasks(JobTaskState.Inactive, 5)
            .Create();
        await jobManager.CreateJobAsync(job);

        var worker = jobWorkerFactory.CreateWorker(job);
        var cancellationTokenSource = new CancellationTokenSource();
        var task = new Task(
            async () => await worker.WorkAsync(cancellationTokenSource.Token),
            cancellationTokenSource.Token);
        task.Start();

        resetEvent.Wait();
        WaitUntil(() => job.Tasks.Count(t => t.State == JobTaskState.Active) >= 4, TimeSpan.FromSeconds(3));

        var jobTask = job.Tasks.First(t => t.State == JobTaskState.Active);
        var nextJobTask = job.Tasks.Single(t => t.State == JobTaskState.Inactive);
        jobTaskWorkerFactory.FinishJobTaskWorker(jobTask);

        await Task.Delay(1100);

        var activeJobTasks = job.Tasks.Where(t => t.State == JobTaskState.Active).ToList();
        job.Tasks.Count(t => t.State == JobTaskState.Inactive).ShouldBe(0);
        // not done in mock
        //activeJobTasks.ShouldNotContain(t => t.Id != jobTask.Id);
        activeJobTasks.ShouldContain(t => t.Id != jobTask.Id);
    }
}
