﻿using AutoMapper;
using Shouldly;
using Wilgysef.Stalk.Application.Contracts.Dtos;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Utilities;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Core.Tests.JobManagerTests;

public class DeactivateJobsAsyncTest : BaseTest
{
    private readonly IJobManager _jobManager;
    private readonly IMapper _mapper;

    public DeactivateJobsAsyncTest()
    {
        _jobManager = GetRequiredService<IJobManager>();
        _mapper = GetRequiredService<IMapper>();
    }

    [Fact]
    public async Task Deactivates_Jobs()
    {
        var jobDtos = new List<JobDto>();

        var jobTaskStates = RandomValues.EnumValues<JobTaskState>();

        foreach (var state in RandomValues.EnumValues<JobState>())
        {
            var builder = new JobBuilder()
                .WithRandomInitializedState(state);

            foreach (var taskState in jobTaskStates)
            {
                builder.WithRandomTasks(taskState, 1);
            }

            var job = builder.Create();
            await _jobManager.CreateJobAsync(job);
            jobDtos.Add(_mapper.Map<JobDto>(job));
        }

        await _jobManager.DeactivateJobsAsync();

        var jobResults = await _jobManager.GetJobsAsync();

        foreach (var result in jobResults)
        {
            var dto = jobDtos.Single(j => j.Id == result.Id.ToString());
            var state = EnumUtils.Parse<JobState>(dto.State);

            switch (state)
            {
                case JobState.Active:
                    result.State.ShouldBe(JobState.Inactive);
                    break;
                case JobState.Cancelling:
                    result.State.ShouldBe(JobState.Cancelled);
                    break;
                case JobState.Pausing:
                    result.State.ShouldBe(JobState.Paused);
                    break;
                default:
                    result.State.ShouldBe(state);
                    break;
            }

            foreach (var task in result.Tasks)
            {
                var taskDto = dto.Tasks.Single(t => t.Id == task.Id.ToString());
                var taskState = EnumUtils.Parse<JobTaskState>(taskDto.State);

                switch (taskState)
                {
                    case JobTaskState.Active:
                        task.State.ShouldBe(JobTaskState.Inactive);
                        break;
                    case JobTaskState.Cancelling:
                        task.State.ShouldBe(JobTaskState.Cancelled);
                        break;
                    case JobTaskState.Pausing:
                        task.State.ShouldBe(JobTaskState.Paused);
                        break;
                    default:
                        task.State.ShouldBe(taskState);
                        break;
                }
            }
        }
    }
}
