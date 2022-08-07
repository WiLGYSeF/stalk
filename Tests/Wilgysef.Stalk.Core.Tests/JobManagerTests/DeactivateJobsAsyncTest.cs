using AutoMapper;
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

        foreach (var state in RandomValues.EnumValues<JobState>())
        {
            var job = new JobBuilder().WithRandomInitializedState(state).Create();
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
        }
    }
}
