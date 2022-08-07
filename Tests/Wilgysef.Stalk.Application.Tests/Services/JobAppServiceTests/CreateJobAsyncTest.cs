using Shouldly;
using Wilgysef.Stalk.Application.Contracts.Commands.Jobs;
using Wilgysef.Stalk.Application.Contracts.Dtos;
using Wilgysef.Stalk.Core.Shared.Cqrs;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Application.Tests.Services.JobAppServiceTests;

public class CreateJobAsyncTest : BaseTest
{
    private readonly ICommandHandler<CreateJob, JobDto> _createJobCommandHandler;

    public CreateJobAsyncTest()
    {
        _createJobCommandHandler = GetRequiredService<ICommandHandler<CreateJob, JobDto>>();
    }

    [Fact]
    public async Task Create_Job()
    {
        var config = new JobConfigDto();
        var command = new CreateJob("test", config, Array.Empty<CreateJobTaskDto>());

        var job = await _createJobCommandHandler.HandleCommandAsync(command);

        job.Name.ShouldBe(command.Name);
        job.State.ShouldBe(JobState.Inactive.ToString().ToLower());
        job.Priority.ShouldBe(0);
        job.Started.ShouldBeNull();
        job.Finished.ShouldBeNull();
        job.DelayedUntil.ShouldBeNull();
        job.Config.ShouldBeEquivalentTo(config);
        job.Tasks.ShouldBeEmpty();
    }
}
