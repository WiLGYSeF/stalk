using Shouldly;
using System.Linq.Expressions;
using Wilgysef.Stalk.Application.Contracts.Dtos;
using Wilgysef.Stalk.Application.Contracts.Queries.Jobs;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Cqrs;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Application.Tests.Queries.Jobs;

public class GetJobsTest : BaseTest
{
    private readonly IQueryHandler<GetJobs, JobListDto> _getJobsQueryHandler;
    private readonly IJobManager _jobManager;

    public GetJobsTest()
    {
        _getJobsQueryHandler = GetRequiredService<IQueryHandler<GetJobs, JobListDto>>();
        _jobManager = GetRequiredService<IJobManager>();
    }

    [Fact]
    public async Task Get_Jobs()
    {
        var jobs = new List<Job>();

        foreach (var state in RandomValues.EnumValues<JobState>())
        {
            jobs.Add(new JobBuilder().WithRandomInitializedState(state).Create());
        }

        foreach (var job in jobs)
        {
            await _jobManager.CreateJobAsync(job);
        }

        var query = await _getJobsQueryHandler.HandleQueryAsync(new GetJobs());

        query.Jobs.Count.ShouldBe(jobs.Count);
    }

    [Fact]
    public async Task Get_Jobs_By_Name()
    {
        var jobs = new List<Job>
        {
            new JobBuilder().WithRandomInitializedState(JobState.Inactive)
                .WithName("test1")
                .Create(),
            new JobBuilder().WithRandomInitializedState(JobState.Inactive)
                .WithName("test2")
                .Create(),
            new JobBuilder().WithRandomInitializedState(JobState.Inactive)
                .WithName("abc")
                .Create(),
        };

        foreach (var job in jobs)
        {
            await _jobManager.CreateJobAsync(job);
        }

        var query = await _getJobsQueryHandler.HandleQueryAsync(new GetJobs(
            "test"));

        query.Jobs.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Get_Jobs_By_State()
    {
        var jobs = new List<Job>();

        foreach (var state in RandomValues.EnumValues<JobState>())
        {
            jobs.Add(new JobBuilder().WithRandomInitializedState(state).Create());
        }

        foreach (var job in jobs)
        {
            await _jobManager.CreateJobAsync(job);
        }

        var query = await _getJobsQueryHandler.HandleQueryAsync(new GetJobs(
            states: new[] { JobState.Inactive.ToString(), JobState.Active.ToString() }));

        query.Jobs.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Get_Jobs_By_StartedBefore()
    {
        var jobs = new List<Job>
        {
            new JobBuilder().WithRandomInitializedState(JobState.Inactive)
                .WithName("this")
                .WithStartedTime(new DateTime(2000, 1, 2))
                .Create(),
            new JobBuilder().WithRandomInitializedState(JobState.Inactive)
                .WithStartedTime(new DateTime(2001, 1, 2))
                .Create(),
            new JobBuilder().WithRandomInitializedState(JobState.Inactive)
                .WithStartedTime(new DateTime(2002, 1, 2))
                .Create(),
        };

        foreach (var job in jobs)
        {
            await _jobManager.CreateJobAsync(job);
        }

        var query = await _getJobsQueryHandler.HandleQueryAsync(new GetJobs(
            startedBefore: new DateTime(2001, 1, 1)));

        query.Jobs.All(j => j.Name == "this").ShouldBeTrue();
    }

    [Fact]
    public async Task Get_Jobs_By_StartedAfter()
    {
        var jobs = new List<Job>
        {
            new JobBuilder().WithRandomInitializedState(JobState.Inactive)
                .WithStartedTime(new DateTime(2000, 1, 2))
                .Create(),
            new JobBuilder().WithRandomInitializedState(JobState.Inactive)
                .WithName("this")
                .WithStartedTime(new DateTime(2001, 1, 2))
                .Create(),
            new JobBuilder().WithRandomInitializedState(JobState.Inactive)
                .WithName("this")
                .WithStartedTime(new DateTime(2002, 1, 2))
                .Create(),
        };

        foreach (var job in jobs)
        {
            await _jobManager.CreateJobAsync(job);
        }

        var query = await _getJobsQueryHandler.HandleQueryAsync(new GetJobs(
            startedAfter: new DateTime(2001, 1, 1)));

        query.Jobs.All(j => j.Name == "this").ShouldBeTrue();
    }

    [Fact]
    public async Task Get_Jobs_By_FinishedBefore()
    {
        var jobs = new List<Job>
        {
            new JobBuilder().WithRandomInitializedState(JobState.Completed)
                .WithName("this")
                .WithStartedTime(new DateTime(2000, 1, 1))
                .WithFinishedTime(new DateTime(2000, 1, 2))
                .Create(),
            new JobBuilder().WithRandomInitializedState(JobState.Completed)
                .WithStartedTime(new DateTime(2001, 1, 1))
                .WithFinishedTime(new DateTime(2001, 1, 2))
                .Create(),
            new JobBuilder().WithRandomInitializedState(JobState.Completed)
                .WithStartedTime(new DateTime(2002, 1, 1))
                .WithFinishedTime(new DateTime(2002, 1, 2))
                .Create(),
        };

        foreach (var job in jobs)
        {
            await _jobManager.CreateJobAsync(job);
        }

        var query = await _getJobsQueryHandler.HandleQueryAsync(new GetJobs(
            finishedBefore: new DateTime(2001, 1, 1)));

        query.Jobs.All(j => j.Name == "this").ShouldBeTrue();
    }

    [Fact]
    public async Task Get_Jobs_By_FinishedAfter()
    {
        var jobs = new List<Job>
        {
            new JobBuilder().WithRandomInitializedState(JobState.Completed)
                .WithStartedTime(new DateTime(2000, 1, 1))
                .WithFinishedTime(new DateTime(2000, 1, 2))
                .Create(),
            new JobBuilder().WithRandomInitializedState(JobState.Completed)
                .WithName("this")
                .WithStartedTime(new DateTime(2001, 1, 1))
                .WithFinishedTime(new DateTime(2001, 1, 2))
                .Create(),
            new JobBuilder().WithRandomInitializedState(JobState.Completed)
                .WithName("this")
                .WithStartedTime(new DateTime(2002, 1, 1))
                .WithFinishedTime(new DateTime(2002, 1, 2))
                .Create(),
        };

        foreach (var job in jobs)
        {
            await _jobManager.CreateJobAsync(job);
        }

        var query = await _getJobsQueryHandler.HandleQueryAsync(new GetJobs(
            finishedAfter: new DateTime(2001, 1, 1)));

        query.Jobs.All(j => j.Name == "this").ShouldBeTrue();
    }

    [Theory]
    [InlineData(JobSortOrder.Id)]
    [InlineData(JobSortOrder.Name)]
    [InlineData(JobSortOrder.State)]
    [InlineData(JobSortOrder.Priority)]
    [InlineData(JobSortOrder.Started)]
    [InlineData(JobSortOrder.Finished)]
    [InlineData(JobSortOrder.TaskCount)]
    public async Task Get_Jobs_Sort_By(JobSortOrder order)
    {
        var jobs = new List<Job>
        {
            new JobBuilder().WithRandomInitializedState(JobState.Completed)
                .WithName("a")
                .WithPriority(3)
                .WithStartedTime(new DateTime(2000, 1, 1))
                .WithFinishedTime(new DateTime(2000, 1, 2))
                .WithRandomTasks(JobTaskState.Inactive, 2)
                .Create(),
            new JobBuilder().WithRandomInitializedState(JobState.Failed)
                .WithName("b")
                .WithPriority(2)
                .WithStartedTime(new DateTime(2001, 1, 1))
                .WithFinishedTime(new DateTime(2001, 1, 2))
                .WithRandomTasks(JobTaskState.Inactive, 1)
                .Create(),
            new JobBuilder().WithRandomInitializedState(JobState.Cancelled)
                .WithName("c")
                .WithPriority(1)
                .WithStartedTime(new DateTime(2002, 1, 1))
                .WithFinishedTime(new DateTime(2002, 1, 2))
                .WithRandomTasks(JobTaskState.Inactive, 3)
                .Create(),
        };

        foreach (var job in jobs)
        {
            await _jobManager.CreateJobAsync(job);
        }

        foreach (var sortDescending in new[] { false, true })
        {
            var query = await _getJobsQueryHandler.HandleQueryAsync(new GetJobs(
                sort: order.ToString(),
                sortDescending: sortDescending));

            Expression<Func<Job, object?>> sort = order switch
            {
                JobSortOrder.Id => j => j.Id,
                JobSortOrder.Name => j => j.Name,
                JobSortOrder.State => j => j.State,
                JobSortOrder.Priority => j => j.Priority,
                JobSortOrder.Started => j => j.Started,
                JobSortOrder.Finished => j => j.Finished,
                JobSortOrder.TaskCount => j => j.Tasks.Count,
                _ => throw new NotImplementedException(),
            };

            var sortedJobs = sortDescending
                ? jobs.AsQueryable().OrderByDescending(sort)
                : jobs.AsQueryable().OrderBy(sort);

            query.Jobs.Select(j => j.Id).ToList()
                .ShouldBeEquivalentTo(sortedJobs.Select(j => j.Id.ToString()).ToList());
        }
    }
}
