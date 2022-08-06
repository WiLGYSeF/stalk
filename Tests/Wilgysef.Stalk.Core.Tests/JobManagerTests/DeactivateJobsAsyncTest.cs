using Shouldly;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Core.Tests.JobManagerTests;

public class DeactivateJobsAsyncTest : BaseTest
{
    private readonly IJobManager _jobManager;

    public DeactivateJobsAsyncTest()
    {
        _jobManager = GetRequiredService<IJobManager>();
    }

    [Fact]
    public async Task Deactivates_Jobs()
    {
        var builder = new JobBuilder();
        var jobs = new List<Job>();

        foreach (var state in RandomValues.EnumValues<JobState>())
        {
            jobs.Add(builder
                .WithRandomId()
                .WithRandomInitializedState(state)
                .Create());
        }

        foreach (var job in jobs)
        {
            await _jobManager.CreateJobAsync(job);
        }

        await _jobManager.DeactivateJobsAsync();

        var jobResults = await _jobManager.GetJobsAsync();

        foreach (var result in jobResults)
        {
            var job = jobs.Single(j => j.Id == result.Id);
            
            switch (job.State)
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
                    result.State.ShouldBe(job.State);
                    break;
            }
        }
    }
}
