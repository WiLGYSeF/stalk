using AutoMapper;
using Shouldly;
using Wilgysef.Stalk.Application.Contracts.Commands.Jobs;
using Wilgysef.Stalk.Application.Contracts.Commands.JobTasks;
using Wilgysef.Stalk.Application.Contracts.Dtos;
using Wilgysef.Stalk.Core.BackgroundJobs;
using Wilgysef.Stalk.Core.Shared.Cqrs;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Application.Tests.Commands.JobTasks;

public class CreateJobTaskTest : BaseTest
{
    private readonly ICommandHandler<CreateJob, JobDto> _createJobCommandHandler;
    private readonly ICommandHandler<CreateJobTask, JobDto> _createJobTaskCommandHandler;
    private readonly IBackgroundJobManager _backgroundJobManager;

    private readonly IMapper _mapper;

    public CreateJobTaskTest()
    {
        _createJobCommandHandler = GetRequiredService<ICommandHandler<CreateJob, JobDto>>();
        _createJobTaskCommandHandler = GetRequiredService<ICommandHandler<CreateJobTask, JobDto>>();
        _backgroundJobManager = GetRequiredService<IBackgroundJobManager>();

        _mapper = GetRequiredService<IMapper>();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Create_JobTask(bool paused)
    {
        var createJobCommand = new CreateJobBuilder(_mapper).WithRandom().Create();

        var job = await _createJobCommandHandler.HandleCommandAsync(createJobCommand);
        var jobId = long.Parse(job.Id);

        var command = new CreateJobTask(
            jobId,
            "test",
            5,
            RandomValues.RandomUri().AbsoluteUri,
            paused ? DateTime.Now.AddDays(1) : null);
        job = await _createJobTaskCommandHandler.HandleCommandAsync(command);

        var jobTask = job.Tasks.Single(t => t.Uri == command.Uri);
        jobTask.Name.ShouldBe(command.Name);
        jobTask.Priority.ShouldBe(command.Priority);
        jobTask.DelayedUntil.ShouldBe(command.DelayedUntil);
    }
}
