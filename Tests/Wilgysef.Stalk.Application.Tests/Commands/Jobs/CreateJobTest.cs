using AutoMapper;
using Shouldly;
using Wilgysef.Stalk.Application.Contracts.Commands.Jobs;
using Wilgysef.Stalk.Application.Contracts.Dtos;
using Wilgysef.Stalk.Core.Shared.Cqrs;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Application.Tests.Commands.Jobs;

public class CreateJobTest : BaseTest
{
    private readonly ICommandHandler<CreateJob, JobDto> _createJobCommandHandler;

    private readonly IMapper _mapper;

    public CreateJobTest()
    {
        _createJobCommandHandler = GetRequiredService<ICommandHandler<CreateJob, JobDto>>();

        _mapper = GetRequiredService<IMapper>();
    }

    [Fact]
    public async Task Create_Job()
    {
        var command = new CreateJobBuilder(_mapper).WithRandom().Create();

        var job = await _createJobCommandHandler.HandleCommandAsync(command);

        job.Name.ShouldBe(command.Name);
        job.State.ShouldBe(JobState.Inactive.ToString().ToLower());
        job.Priority.ShouldBe(command.Priority);
        job.Started.ShouldBeNull();
        job.Finished.ShouldBeNull();
        job.DelayedUntil.ShouldBeNull();
        job.Config.ShouldBeEquivalentTo(command.Config);

        job.Tasks.Count.ShouldBe(command.Tasks.Count);
        foreach (var task in job.Tasks)
        {
            var expectedTask = command.Tasks.Single(t => t.Uri == task.Uri);
            task.Name.ShouldBe(expectedTask.Name);
            task.State.ShouldBe(JobTaskState.Inactive.ToString().ToLower());
            task.Priority.ShouldBe(expectedTask.Priority);
            task.Uri.ShouldBe(expectedTask.Uri);
            task.Type.ShouldBe(JobTaskType.Extract.ToString().ToLower());
            task.Started.ShouldBeNull();
            task.Finished.ShouldBeNull();
            task.DelayedUntil.ShouldBeNull();
            task.Result.Success.ShouldBeNull();
            task.Result.ErrorCode.ShouldBeNull();
            task.Result.ErrorMessage.ShouldBeNull();
            task.Result.ErrorDetail.ShouldBeNull();
        }
    }

    [Fact]
    public async Task Create_Job_No_Tasks()
    {
        var command = new CreateJobBuilder(_mapper)
            .Create();

        var job = await _createJobCommandHandler.HandleCommandAsync(command);

        job.Name.ShouldBe(command.Name);
        job.State.ShouldBe(JobState.Inactive.ToString().ToLower());
        job.Priority.ShouldBe(command.Priority);
        job.Started.ShouldBeNull();
        job.Finished.ShouldBeNull();
        job.DelayedUntil.ShouldBeNull();
        job.Config.ShouldBeEquivalentTo(command.Config);
        job.Tasks.ShouldBeEmpty();
    }

    [Fact]
    public async Task Create_Job_Delayed_Paused()
    {
        var command = new CreateJobBuilder(_mapper)
            .WithDelayedUntil(DateTime.Now.AddHours(1))
            .Create();

        var job = await _createJobCommandHandler.HandleCommandAsync(command);

        job.Name.ShouldBe(command.Name);
        job.State.ShouldBe(JobState.Paused.ToString().ToLower());
        job.Priority.ShouldBe(command.Priority);
        job.Started.ShouldBeNull();
        job.Finished.ShouldBeNull();
        job.DelayedUntil.ShouldBe(command.DelayedUntil);
        job.Config.ShouldBeEquivalentTo(command.Config);
        job.Tasks.ShouldBeEmpty();
    }

    [Fact]
    public async Task Create_Job_Task_Delayed_Paused()
    {
        var command = new CreateJobBuilder(_mapper)
            .WithRandom()
            .Create();

        foreach (var task in command.Tasks)
        {
            task.DelayedUntil = DateTime.Now.AddHours(1);
        }

        var job = await _createJobCommandHandler.HandleCommandAsync(command);

        job.Name.ShouldBe(command.Name);
        job.State.ShouldBe(JobState.Inactive.ToString().ToLower());
        job.Priority.ShouldBe(command.Priority);
        job.Started.ShouldBeNull();
        job.Finished.ShouldBeNull();
        job.DelayedUntil.ShouldBeNull();
        job.Config.ShouldBeEquivalentTo(command.Config);

        job.Tasks.Count.ShouldBe(command.Tasks.Count);
        foreach (var task in job.Tasks)
        {
            var expectedTask = command.Tasks.Single(t => t.Name == task.Name);
            task.Name.ShouldBe(expectedTask.Name);
            task.State.ShouldBe(JobTaskState.Paused.ToString().ToLower());
            task.Priority.ShouldBe(expectedTask.Priority);
            task.Uri.ShouldBe(expectedTask.Uri);
            task.Type.ShouldBe(JobTaskType.Extract.ToString().ToLower());
            task.Started.ShouldBeNull();
            task.Finished.ShouldBeNull();
            task.DelayedUntil.ShouldBe(expectedTask.DelayedUntil);
            task.Result.Success.ShouldBeNull();
            task.Result.ErrorCode.ShouldBeNull();
            task.Result.ErrorMessage.ShouldBeNull();
            task.Result.ErrorDetail.ShouldBeNull();
        }
    }
}
