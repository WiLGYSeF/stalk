using Shouldly;
using Wilgysef.Stalk.Application.Contracts.Dtos;
using Wilgysef.Stalk.Application.Contracts.Queries.Jobs;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Cqrs;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Application.Tests.Queries.Jobs;

public class GetJobTest : BaseTest
{
    private readonly IQueryHandler<GetJob, JobDto> _getJobQueryHandler;
    private readonly IJobManager _jobManager;

    public GetJobTest()
    {
        _getJobQueryHandler = GetRequiredService<IQueryHandler<GetJob, JobDto>>();
        _jobManager = GetRequiredService<IJobManager>();
    }

    [Fact]
    public async Task Get_Job()
    {
        var job = new JobBuilder().WithRandomInitializedState(JobState.Inactive)
            .WithRandomTasks(JobTaskState.Inactive, 3)
            .Create();

        await _jobManager.CreateJobAsync(job);

        var jobResult = await _getJobQueryHandler.HandleQueryAsync(new GetJob(job.Id));

        jobResult.Name.ShouldBe(job.Name);
        jobResult.State.ShouldBe(job.State.ToString().ToLower());
        jobResult.Priority.ShouldBe(job.Priority);
        jobResult.Started.ShouldBe(job.Started);
        jobResult.Finished.ShouldBe(job.Finished);
        jobResult.DelayedUntil.ShouldBe(job.DelayedUntil);

        jobResult.Tasks.Count.ShouldBe(job.Tasks.Count);
        foreach (var task in jobResult.Tasks)
        {
            var expectedTask = jobResult.Tasks.Single(t => t.Uri == task.Uri);
            task.Name.ShouldBe(expectedTask.Name);
            task.State.ShouldBe(expectedTask.State.ToString().ToLower());
            task.Priority.ShouldBe(expectedTask.Priority);
            task.Uri.ShouldBe(expectedTask.Uri);
            task.Type.ShouldBe(expectedTask.Type.ToString().ToLower());
            task.Started.ShouldBe(expectedTask.Started);
            task.Finished.ShouldBe(expectedTask.Finished);
            task.DelayedUntil.ShouldBe(expectedTask.DelayedUntil);
            task.Result.Success.ShouldBe(expectedTask.Result.Success);
            task.Result.ErrorCode.ShouldBe(expectedTask.Result.ErrorCode);
            task.Result.ErrorMessage.ShouldBe(expectedTask.Result.ErrorMessage);
            task.Result.ErrorDetail.ShouldBe(expectedTask.Result.ErrorDetail);
        }
    }
}
