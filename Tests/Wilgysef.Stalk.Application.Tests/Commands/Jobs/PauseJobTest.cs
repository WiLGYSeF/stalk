﻿using Autofac;
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

public class PauseJobTest : BaseTest
{
    private readonly ICommandHandler<CreateJob, JobDto> _createJobCommandHandler;
    private readonly ICommandHandler<PauseJob, JobDto> _pauseJobCommandHandler;

    private readonly JobStarter _jobStarter;
    private readonly IMapper _mapper;

    public PauseJobTest()
    {
        ReplaceSingletonService<IJobTaskWorkerFactory>(c => new JobTaskWorkerFactoryMock(
            c.Resolve<IServiceLocator>()));

        _createJobCommandHandler = GetRequiredService<ICommandHandler<CreateJob, JobDto>>();
        _pauseJobCommandHandler = GetRequiredService<ICommandHandler<PauseJob, JobDto>>();

        _jobStarter = new JobStarter(BeginLifetimeScope());
        _mapper = GetRequiredService<IMapper>();
    }

    [Fact]
    public async Task Pause_Job()
    {
        var createCommand = new CreateJobBuilder(_mapper).WithRandom().Create();

        var jobDto = await _createJobCommandHandler.HandleCommandAsync(createCommand);
        var jobId = long.Parse(jobDto.Id);

        await _jobStarter.WorkPrioritizedJobsAsync();

        var job = await this.WaitUntilJobAsync(jobId, job => job.IsActive);
        job.State.ShouldBe(JobState.Active);

        await _pauseJobCommandHandler.HandleCommandAsync(new PauseJob(jobId));

        job = await this.WaitUntilJobAsync(jobId, job => !job.IsActive);
        job.State.ShouldBe(JobState.Paused);
    }
}
