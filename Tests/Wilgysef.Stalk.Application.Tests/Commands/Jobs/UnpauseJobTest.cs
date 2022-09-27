using Autofac;
using AutoMapper;
using Shouldly;
using Wilgysef.Stalk.Application.Contracts.Commands.Jobs;
using Wilgysef.Stalk.Application.Contracts.Dtos;
using Wilgysef.Stalk.Application.Tests.Utilities;
using Wilgysef.Stalk.Core.JobTaskWorkerFactories;
using Wilgysef.Stalk.Core.Shared.Cqrs;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;
using Wilgysef.Stalk.TestBase;
using Wilgysef.Stalk.TestBase.Extensions;
using Wilgysef.Stalk.TestBase.Mocks;

namespace Wilgysef.Stalk.Application.Tests.Commands.Jobs;

public class UnpauseJobTest : BaseTest
{
    private readonly ICommandHandler<CreateJob, JobDto> _createJobCommandHandler;
    private readonly ICommandHandler<UnpauseJob, JobDto> _unpauseJobCommandHandler;

    private readonly JobStarter _jobStarter;
    private readonly IMapper _mapper;

    public UnpauseJobTest()
    {
        ReplaceSingletonService<IJobTaskWorkerFactory>(c => new JobTaskWorkerFactoryMock(
            c.Resolve<IServiceLocator>()));

        _createJobCommandHandler = GetRequiredService<ICommandHandler<CreateJob, JobDto>>();
        _unpauseJobCommandHandler = GetRequiredService<ICommandHandler<UnpauseJob, JobDto>>();

        _jobStarter = new JobStarter(BeginLifetimeScope());
        _mapper = GetRequiredService<IMapper>();
    }

    [Fact]
    public async Task Unpause_Job()
    {
        var createCommand = new CreateJobBuilder(_mapper)
            .WithRandom()
            .WithDelayedUntil(DateTime.Now.AddDays(1))
            .Create();

        var jobDto = await _createJobCommandHandler.HandleCommandAsync(createCommand);
        var jobId = long.Parse(jobDto.Id);

        await _jobStarter.WorkPrioritizedJobsAsync();

        var job = await this.WaitUntilJobAsync(jobId, job => job.State == JobState.Paused);
        job.State.ShouldBe(JobState.Paused);

        await _unpauseJobCommandHandler.HandleCommandAsync(new UnpauseJob(jobId));

        job = await this.WaitUntilJobAsync(jobId, job => job.State == JobState.Inactive);
        job.State.ShouldBe(JobState.Inactive);
    }
}
