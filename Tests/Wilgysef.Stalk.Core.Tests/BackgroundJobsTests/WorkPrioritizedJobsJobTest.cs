using Shouldly;
using Wilgysef.Stalk.Core.BackgroundJobs;
using Wilgysef.Stalk.Core.BackgroundJobs.Args;
using Wilgysef.Stalk.Core.JobWorkers;
using Wilgysef.Stalk.Core.JobWorkerServices;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.TestBase;
using Wilgysef.Stalk.TestBase.Extensions;
using Wilgysef.Stalk.TestBase.Mocks;

namespace Wilgysef.Stalk.Core.Tests.BackgroundJobsTests;

public class WorkPrioritizedJobsJobTest : BaseTest
{
    private readonly IBackgroundJobHandler<WorkPrioritizedJobsArgs> _workPrioritizedJobsHandler;
    private readonly IJobManager _jobManager;
    private readonly IJobWorkerService _jobWorkerService;
    private readonly IJobWorkerCollectionService _jobWorkerCollectionService;

    public WorkPrioritizedJobsJobTest()
    {
        ReplaceService<IJobWorkerFactory, JobWorkerFactoryMock>();

        _workPrioritizedJobsHandler = GetRequiredService<IBackgroundJobHandler<WorkPrioritizedJobsArgs>>();
        _jobManager = GetRequiredService<IJobManager>();
        _jobWorkerService = GetRequiredService<IJobWorkerService>();
        _jobWorkerCollectionService = GetRequiredService<IJobWorkerCollectionService>();
    }

    [Fact]
    public async Task No_Queued_Jobs()
    {
        await _workPrioritizedJobsHandler.ExecuteJobAsync(new WorkPrioritizedJobsArgs());

        _jobWorkerCollectionService.Workers.ShouldBeEmpty();
    }

    [Fact]
    public async Task No_Queued_Jobs_With_Active()
    {
        var job = new JobBuilder()
            .WithRandomInitializedState(JobState.Inactive)
            .Create();
        await _jobManager.CreateJobAsync(job);

        await _jobWorkerService.StartJobWorkerAsync(job);

        await _workPrioritizedJobsHandler.ExecuteJobAsync(new WorkPrioritizedJobsArgs());

        _jobWorkerCollectionService.Workers.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Activate_Jobs()
    {
        var jobs = new Queue<Job>(new[]
        {
            new JobBuilder()
                .WithRandomInitializedState(JobState.Inactive)
                .WithRandomTasks(JobTaskState.Inactive, 2)
                .Create(),
            new JobBuilder()
                .WithRandomInitializedState(JobState.Inactive)
                .WithRandomTasks(JobTaskState.Inactive, 2)
                .Create(),
            new JobBuilder()
                .WithRandomInitializedState(JobState.Inactive)
                .WithRandomTasks(JobTaskState.Inactive, 2)
                .Create(),
            new JobBuilder()
                .WithRandomInitializedState(JobState.Inactive)
                .WithRandomTasks(JobTaskState.Inactive, 2)
                .Create(),
            new JobBuilder()
                .WithRandomInitializedState(JobState.Inactive)
                .WithRandomTasks(JobTaskState.Inactive, 2)
                .Create(),
            new JobBuilder()
                .WithRandomInitializedState(JobState.Inactive)
                .WithRandomTasks(JobTaskState.Inactive, 2)
                .Create(),
        });
        foreach (var j in jobs)
        {
            await _jobManager.CreateJobAsync(j);
        }

        var job = jobs.Dequeue();
        await _jobWorkerService.StartJobWorkerAsync(job);
        _jobWorkerCollectionService.Workers.Count.ShouldBe(1);

        job = await this.WaitUntilJobAsync(job.Id, job => job.IsActive);

        await _workPrioritizedJobsHandler.ExecuteJobAsync(new WorkPrioritizedJobsArgs());
        _jobWorkerCollectionService.Workers.Count.ShouldBe(4);
    }

    [Fact]
    public async Task Activate_Jobs_Replace_Lowest_Priority()
    {
        var jobs = new[]
        {
            new JobBuilder()
                .WithRandomInitializedState(JobState.Inactive)
                .WithPriority(10)
                .WithRandomTasks(JobTaskState.Inactive, 2)
                .Create(),
            new JobBuilder()
                .WithRandomInitializedState(JobState.Inactive)
                .WithPriority(20)
                .WithRandomTasks(JobTaskState.Inactive, 2)
                .Create(),
            new JobBuilder()
                .WithRandomInitializedState(JobState.Inactive)
                .WithPriority(30)
                .WithRandomTasks(JobTaskState.Inactive, 2)
                .Create(),
            new JobBuilder()
                .WithRandomInitializedState(JobState.Inactive)
                .WithPriority(40)
                .WithRandomTasks(JobTaskState.Inactive, 2)
                .Create(),
        };

        var lowestPriorityJob = jobs.OrderBy(j => j.Priority).First();

        foreach (var job in jobs)
        {
            await _jobManager.CreateJobAsync(job);
        }

        foreach (var job in jobs)
        {
            await _jobWorkerService.StartJobWorkerAsync(job);
        }

        lowestPriorityJob = await this.WaitUntilJobAsync(lowestPriorityJob.Id, job => job.IsActive);

        var newJob = new JobBuilder()
            .WithRandomInitializedState(JobState.Inactive)
            .WithPriority(25)
            .WithRandomTasks(JobTaskState.Inactive, 2)
            .Create();
        await _jobManager.CreateJobAsync(newJob);

        await _workPrioritizedJobsHandler.ExecuteJobAsync(new WorkPrioritizedJobsArgs());

        _jobWorkerCollectionService.Workers.Count.ShouldBe(4);
        _jobWorkerCollectionService.Workers.ShouldContain(w => w.Job.Id == newJob.Id);
        _jobWorkerCollectionService.Workers.ShouldNotContain(w => w.Job.Id == lowestPriorityJob.Id);
    }

    [Fact]
    public async Task Activate_Jobs_Replaces_Nothing()
    {
        var jobs = new[]
        {
            new JobBuilder()
                .WithRandomInitializedState(JobState.Inactive)
                .WithPriority(10)
                .WithRandomTasks(JobTaskState.Inactive, 2)
                .Create(),
            new JobBuilder()
                .WithRandomInitializedState(JobState.Inactive)
                .WithPriority(20)
                .WithRandomTasks(JobTaskState.Inactive, 2)
                .Create(),
            new JobBuilder()
                .WithRandomInitializedState(JobState.Inactive)
                .WithPriority(30)
                .WithRandomTasks(JobTaskState.Inactive, 2)
                .Create(),
            new JobBuilder()
                .WithRandomInitializedState(JobState.Inactive)
                .WithPriority(40)
                .WithRandomTasks(JobTaskState.Inactive, 2)
                .Create(),
        };

        var lowestPriorityJob = jobs.OrderBy(j => j.Priority).First();

        foreach (var job in jobs)
        {
            await _jobManager.CreateJobAsync(job);
        }

        foreach (var job in jobs)
        {
            await _jobWorkerService.StartJobWorkerAsync(job);
        }

        lowestPriorityJob = await this.WaitUntilJobAsync(lowestPriorityJob.Id, job => job.IsActive);

        var newJob = new JobBuilder()
            .WithRandomInitializedState(JobState.Inactive)
            .WithPriority(-10)
            .WithRandomTasks(JobTaskState.Inactive, 2)
            .Create();
        await _jobManager.CreateJobAsync(newJob);

        await _workPrioritizedJobsHandler.ExecuteJobAsync(new WorkPrioritizedJobsArgs());

        _jobWorkerCollectionService.Workers.Count.ShouldBe(4);
        _jobWorkerCollectionService.Workers.ShouldNotContain(w => w.Job.Id == newJob.Id);
    }
}
