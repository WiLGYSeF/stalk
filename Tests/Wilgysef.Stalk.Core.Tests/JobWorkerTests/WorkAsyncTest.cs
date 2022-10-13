using Autofac;
using Shouldly;
using System.Net;
using Wilgysef.Stalk.Core.JobTaskWorkerFactories;
using Wilgysef.Stalk.Core.JobWorkerFactories;
using Wilgysef.Stalk.Core.JobWorkerServices;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;
using Wilgysef.Stalk.Core.Tests.Utilities;
using Wilgysef.Stalk.TestBase;
using Wilgysef.Stalk.TestBase.Extensions;
using Wilgysef.Stalk.TestBase.Mocks;

namespace Wilgysef.Stalk.Core.Tests.JobWorkerTests;

public class WorkAsyncTest : BaseTest
{
    private const int TaskWorkerLimitDefault = 4;

    private readonly JobTaskWorkerFactoryMock _jobTaskWorkerFactory;
    private readonly IJobManager _jobManager;
    private readonly IJobWorkerFactory _jobWorkerFactory;

    private readonly JobWorkerStarter _jobWorkerStarter;

    public WorkAsyncTest()
    {
        ReplaceSingletonService<IJobTaskWorkerFactory>(c => new JobTaskWorkerFactoryMock(
            c.Resolve<IServiceLocator>()));

        _jobTaskWorkerFactory = (JobTaskWorkerFactoryMock)GetRequiredService<IJobTaskWorkerFactory>();

        _jobManager = GetRequiredService<IJobManager>();
        _jobWorkerFactory = GetRequiredService<IJobWorkerFactory>();

        HttpClientInterceptor!.AddForAny((request, cancellationToken) =>
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new MemoryStream())
            });
        });

        _jobWorkerStarter = new JobWorkerStarter(_jobWorkerFactory);
    }

    [Fact]
    public async Task Work_Job_Start_Tasks()
    {
        var job = new JobBuilder()
            .WithRandomInitializedState(JobState.Inactive)
            .WithRandomTasks(JobTaskState.Inactive, TaskWorkerLimitDefault - 1)
            .Create();
        await _jobManager.CreateJobAsync(job);

        using var workerInstance = _jobWorkerStarter.CreateAndStartWorker(job);

        job = await this.WaitUntilJobAsync(job.Id, job => job.State == JobState.Active);
    }

    [Fact]
    public async Task Work_Job_Wait_Tasks()
    {
        Job job;
        using (var scope = BeginLifetimeScope())
        {
            job = new JobBuilder()
                .WithRandomInitializedState(JobState.Inactive)
                .WithRandomTasks(JobTaskState.Inactive, TaskWorkerLimitDefault + 1)
                .Create();
            var jobManager = scope.GetRequiredService<IJobManager>();
            await jobManager.CreateJobAsync(job);
        }

        using var workerInstance = _jobWorkerStarter.CreateAndStartWorker(job);

        job = await this.WaitUntilJobAsync(
            job.Id,
            job => job.Tasks.Count(t => t.State == JobTaskState.Active) >= workerInstance.Worker.WorkerLimit);

        job.Tasks.Count(t => t.State == JobTaskState.Active).ShouldBe(workerInstance.Worker.WorkerLimit);
        job.Tasks.Count(t => t.State == JobTaskState.Inactive).ShouldBe(job.Tasks.Count - workerInstance.Worker.WorkerLimit);
    }

    [Fact]
    public async Task Work_Job_Cycle_Tasks()
    {
        Job job;
        using (var scope = BeginLifetimeScope())
        {
            job = new JobBuilder()
                .WithRandomInitializedState(JobState.Inactive)
                .WithRandomTasks(JobTaskState.Inactive, TaskWorkerLimitDefault + 1)
                .Create();
            await _jobManager.CreateJobAsync(job);
        }

        using var workerInstance = _jobWorkerStarter.CreateAndStartWorker(job);

        job = await this.WaitUntilJobAsync(
            job.Id,
            job => job.Tasks.Count(t => t.State == JobTaskState.Active) >= workerInstance.Worker.WorkerLimit,
            TimeSpan.FromSeconds(5));

        job.Tasks.Count(t => t.State == JobTaskState.Active).ShouldBe(workerInstance.Worker.WorkerLimit);

        var jobTask = job.Tasks.First(t => t.State == JobTaskState.Active);
        var nextJobTask = job.Tasks.Single(t => t.State == JobTaskState.Inactive);
        _jobTaskWorkerFactory.FinishJobTaskWorker(jobTask);

        await Task.Delay(workerInstance.Worker.TaskWaitTimeout * 2);
        workerInstance.WorkerTask.Exception.ShouldBeNull();

        job = await this.ReloadJobAsync(job.Id);

        var activeJobTasks = job.Tasks.Where(t => t.State == JobTaskState.Active).ToList();
        activeJobTasks.Count.ShouldBe(workerInstance.Worker.WorkerLimit);
        job.Tasks.Count(t => t.State == JobTaskState.Inactive).ShouldBe(0);
        activeJobTasks.ShouldNotContain(t => t.Id == jobTask.Id);
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

        job = await this.WaitUntilJobAsync(job.Id, job => job.Tasks.Any(t => t.State == JobTaskState.Active));

        var jobTask = job.Tasks.Single(t => t.State == JobTaskState.Active);
        _jobTaskWorkerFactory.FinishJobTaskWorker(jobTask);

        job = await this.WaitUntilJobAsync(job.Id, job => job.State == JobState.Completed);

        var jobWorkerCollectionService = GetRequiredService<IJobWorkerCollectionService>();
        jobWorkerCollectionService.Workers.ShouldBeEmpty();
    }

    [Fact]
    public async Task Work_Job_Fail_Tasks()
    {
        var job = new JobBuilder()
            .WithRandomInitializedState(JobState.Inactive)
            .WithConfig(new JobConfig
            {
                MaxFailures = 1,
            })
            .WithRandomTasks(JobTaskState.Inactive, 2)
            .Create();
        await _jobManager.CreateJobAsync(job);

        _jobWorkerStarter.EnsureTaskSuccessesOnDispose = false;
        using var workerInstance = _jobWorkerStarter.CreateAndStartWorker(job);

        job = await this.WaitUntilJobAsync(
            job.Id,
            job => job.Tasks.Count(t => t.State == JobTaskState.Active) >= 2,
            shouldCheckCondition: false);
        workerInstance.WorkerTask.Exception.ShouldBeNull();

        job.Tasks.Count(t => t.State == JobTaskState.Active).ShouldBeGreaterThanOrEqualTo(2);

        foreach (var jobTask in job.Tasks)
        {
            _jobTaskWorkerFactory.FailJobTaskWorker(jobTask);
        }

        job = await this.WaitUntilJobAsync(job.Id, job => job.State == JobState.Failed);

        var jobWorkerCollectionService = GetRequiredService<IJobWorkerCollectionService>();
        jobWorkerCollectionService.Workers.ShouldBeEmpty();
    }

    [Fact]
    public async Task Work_Job_Fail_Tasks_Retry()
    {
        var job = new JobBuilder()
            .WithRandomInitializedState(JobState.Inactive)
            .WithConfig(new JobConfig
            {
                MaxFailures = 1,
            })
            .WithRandomTasks(JobTaskState.Inactive, 1)
            .Create();
        await _jobManager.CreateJobAsync(job);

        _jobWorkerStarter.EnsureTaskSuccessesOnDispose = false;
        using var workerInstance = _jobWorkerStarter.CreateAndStartWorker(job);

        job = await this.WaitUntilJobAsync(
            job.Id,
            job => job.Tasks.Any(t => t.State == JobTaskState.Active),
            shouldCheckCondition: false);
        workerInstance.WorkerTask.Exception.ShouldBeNull();

        job.Tasks.Count(t => t.State == JobTaskState.Active).ShouldBeGreaterThanOrEqualTo(1);

        var jobTask = job.Tasks.Single(t => t.State == JobTaskState.Active);
        _jobTaskWorkerFactory.FailJobTaskWorker(jobTask, new HttpRequestException(null, null, HttpStatusCode.InternalServerError));

        job = await this.WaitUntilJobAsync(
            job.Id,
            job => job.Tasks.Count >= 2 && job.Tasks.Any(t => t.Id != jobTask.Id && t.State == JobTaskState.Active));
        workerInstance.WorkerTask.Exception.ShouldBeNull();

        jobTask = job.Tasks.Single(t => t.Id != jobTask.Id);
        _jobTaskWorkerFactory.FailJobTaskWorker(jobTask, new HttpRequestException(null, null, HttpStatusCode.InternalServerError));

        job = await this.WaitUntilJobAsync(job.Id, job => job.State == JobState.Failed);
        workerInstance.WorkerTask.Exception.ShouldBeNull();

        job.State.ShouldBe(JobState.Failed);
        job.Tasks.Count.ShouldBe(3);

        var jobWorkerCollectionService = GetRequiredService<IJobWorkerCollectionService>();
        jobWorkerCollectionService.Workers.ShouldBeEmpty();
    }

    [Fact]
    public async Task Work_Job_Pause_All_Tasks_Paused()
    {
        var job = new JobBuilder()
            .WithRandomInitializedState(JobState.Inactive)
            .WithRandomTasks(JobTaskState.Paused, 2)
            .Create();
        await _jobManager.CreateJobAsync(job);

        _jobWorkerStarter.EnsureTaskSuccessesOnDispose = false;
        using var workerInstance = _jobWorkerStarter.CreateAndStartWorker(job);

        job = await this.WaitUntilJobAsync(job.Id, job => !job.IsActive);
        workerInstance.WorkerTask.Exception.ShouldBeNull();

        job.State.ShouldBe(JobState.Inactive);

        var jobWorkerCollectionService = GetRequiredService<IJobWorkerCollectionService>();
        jobWorkerCollectionService.Workers.ShouldBeEmpty();
    }

    [Fact]
    public async Task Work_Job_Cancel()
    {
        // TODO: unstable test

        var job = new JobBuilder()
            .WithRandomInitializedState(JobState.Inactive)
            .WithRandomTasks(JobTaskState.Inactive, 1)
            .Create();
        await _jobManager.CreateJobAsync(job);

        using var workerInstance = _jobWorkerStarter.CreateAndStartWorker(job);

        job = await this.WaitUntilJobAsync(job.Id, job => job.Tasks.Any(t => t.State == JobTaskState.Active));
        job.Tasks.Count(t => t.State == JobTaskState.Active).ShouldBe(1);

        workerInstance.CancellationTokenSource.Cancel();

        job = await this.WaitUntilJobAsync(job.Id, job => job.State == JobState.Inactive);

        // TODO: remove
        var customMessage = string.Join(
            Environment.NewLine,
            job.Tasks.Select(t => $"{t.Id} {t.State}"));

        job.State.ShouldBe(JobState.Inactive, customMessage);

        var jobWorkerCollectionService = GetRequiredService<IJobWorkerCollectionService>();
        jobWorkerCollectionService.Workers.ShouldBeEmpty();
    }
}
