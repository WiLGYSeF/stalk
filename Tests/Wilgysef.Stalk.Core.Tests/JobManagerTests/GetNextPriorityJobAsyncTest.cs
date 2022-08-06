using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Core.Tests.JobManagerTests;

public class GetNextPriorityJobAsyncTest : BaseTest
{
    private readonly IJobManager _jobManager;

    public GetNextPriorityJobAsyncTest()
    {
        _jobManager = GetRequiredService<IJobManager>();
    }

    [Fact]
    public async Task Get_Next_Job_By_Priority()
    {
        var jobs = new List<Job>
        {
            new JobBuilder().WithRandomInitializedState(JobState.Inactive)
                .WithPriority(5)
                .WithRandomTasks(JobTaskState.Inactive, 3)
                .Create(),
            new JobBuilder().WithRandomInitializedState(JobState.Inactive)
                .WithPriority(15)
                .WithRandomTasks(JobTaskState.Inactive, 3)
                .Create(),
            new JobBuilder().WithRandomInitializedState(JobState.Inactive)
                .WithPriority(0)
                .WithRandomTasks(JobTaskState.Inactive, 3)
                .Create(),
            new JobBuilder().WithRandomInitializedState(JobState.Inactive)
                .WithPriority(32)
                .WithRandomTasks(JobTaskState.Inactive, 3)
                .Create(),
            new JobBuilder().WithRandomInitializedState(JobState.Inactive)
                .WithPriority(-4)
                .WithRandomTasks(JobTaskState.Inactive, 3)
                .Create(),
        };
        var expectedJobs = new[] { jobs[3], jobs[1], jobs[0], jobs[2], jobs[4] };

        foreach (var job in jobs)
        {
            await _jobManager.CreateJobAsync(job);
        }

        for (var i = 0; i < expectedJobs.Length; i++)
        {
            var next = await _jobManager.GetNextPriorityJobAsync();
            next.ShouldNotBeNull();

            next.Id.ShouldBe(expectedJobs[i].Id);
            await _jobManager.DeleteJobAsync(next);
        }
    }

    [Fact]
    public async Task Get_Next_Job_Same_Priority_By_Started()
    {
        var now = DateTime.Now;

        var jobs = new List<Job>
        {
            new JobBuilder().WithRandomInitializedState(JobState.Inactive)
                .WithStartedTime(now.AddMinutes(7))
                .WithRandomTasks(JobTaskState.Inactive, 3)
                .Create(),
            new JobBuilder().WithRandomInitializedState(JobState.Inactive)
                .WithStartedTime(now.AddMinutes(5))
                .WithRandomTasks(JobTaskState.Inactive, 3)
                .Create(),
            new JobBuilder().WithRandomInitializedState(JobState.Inactive)
                .WithStartedTime(now.AddMinutes(10))
                .WithRandomTasks(JobTaskState.Inactive, 3)
                .Create(),
            new JobBuilder().WithRandomInitializedState(JobState.Inactive)
                .WithStartedTime(now.AddMinutes(1))
                .WithRandomTasks(JobTaskState.Inactive, 3)
                .Create(),
            new JobBuilder().WithRandomInitializedState(JobState.Inactive)
                .WithStartedTime(now.AddMinutes(15))
                .WithRandomTasks(JobTaskState.Inactive, 3)
                .Create(),
        };
        var expectedJobs = new[] { jobs[3], jobs[1], jobs[0], jobs[2], jobs[4] };

        foreach (var job in jobs)
        {
            await _jobManager.CreateJobAsync(job);
        }

        for (var i = 0; i < expectedJobs.Length; i++)
        {
            var next = await _jobManager.GetNextPriorityJobAsync();
            next.ShouldNotBeNull();

            next.Id.ShouldBe(expectedJobs[i].Id);
            await _jobManager.DeleteJobAsync(next);
        }
    }

    [Fact]
    public async Task Get_Next_Job_Must_Be_Queued()
    {
        var jobs = new List<Job>();

        foreach (var state in RandomValues.EnumValues<JobState>())
        {
            jobs.Add(new JobBuilder().WithRandomInitializedState(state)
                .WithPriority(state == JobState.Inactive ? -10 : 0)
                .WithRandomTasks(JobTaskState.Inactive, 3)
                .Create());
        };

        foreach (var job in jobs)
        {
            await _jobManager.CreateJobAsync(job);
        }

        var next = await _jobManager.GetNextPriorityJobAsync();
        next.ShouldNotBeNull();

        next.Id.ShouldBe(jobs.Single(j => j.State == JobState.Inactive).Id);
    }


    [Fact]
    public async Task Get_Next_Job_Must_Have_Tasks_Queued()
    {
        var jobs = new List<Job>
        {
            new JobBuilder().WithRandomInitializedState(JobState.Inactive)
                .Create(),
            new JobBuilder().WithRandomInitializedState(JobState.Inactive)
                .WithRandomTasks(JobTaskState.Completed, 3)
                .Create(),
            new JobBuilder().WithRandomInitializedState(JobState.Inactive)
                .WithPriority(-10)
                .WithRandomTasks(JobTaskState.Inactive, 3)
                .Create(),
        };

        foreach (var job in jobs)
        {
            await _jobManager.CreateJobAsync(job);
        }

        var next = await _jobManager.GetNextPriorityJobAsync();
        next.ShouldNotBeNull();

        next.Id.ShouldBe(jobs[2].Id);
    }
}
