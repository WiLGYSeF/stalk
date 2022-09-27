﻿using Autofac;
using AutoMapper;
using Shouldly;
using Wilgysef.Stalk.Application.Contracts.Commands.Jobs;
using Wilgysef.Stalk.Application.Contracts.Commands.JobTasks;
using Wilgysef.Stalk.Application.Contracts.Dtos;
using Wilgysef.Stalk.Application.Tests.Utilities;
using Wilgysef.Stalk.Core.JobTaskWorkerFactories;
using Wilgysef.Stalk.Core.Shared.Cqrs;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;
using Wilgysef.Stalk.TestBase;
using Wilgysef.Stalk.TestBase.Extensions;
using Wilgysef.Stalk.TestBase.Mocks;

namespace Wilgysef.Stalk.Application.Tests.Commands.JobTasks;

public class StopJobTaskTest : BaseTest
{
    private readonly ICommandHandler<CreateJob, JobDto> _createJobCommandHandler;
    private readonly ICommandHandler<StopJobTask, JobDto> _stopJobTaskCommandHandler;

    private readonly JobStarter _jobStarter;
    private readonly IMapper _mapper;

    public StopJobTaskTest()
    {
        ReplaceSingletonService<IJobTaskWorkerFactory>(c => new JobTaskWorkerFactoryMock(
            c.Resolve<IServiceLocator>()));

        _createJobCommandHandler = GetRequiredService<ICommandHandler<CreateJob, JobDto>>();
        _stopJobTaskCommandHandler = GetRequiredService<ICommandHandler<StopJobTask, JobDto>>();

        _jobStarter = new JobStarter(BeginLifetimeScope());
        _mapper = GetRequiredService<IMapper>();
    }

    [Fact]
    public async Task Stop_JobTask()
    {
        var createCommand = new CreateJobBuilder(_mapper).WithRandom().Create();

        var jobDto = await _createJobCommandHandler.HandleCommandAsync(createCommand);
        var jobId = long.Parse(jobDto.Id);

        await _jobStarter.WorkPrioritizedJobsAsync();

        var job = await this.WaitUntilJobAsync(jobId, job => job.Tasks.Any(t => t.IsActive));
        var jobTaskId = job.Tasks.First(t => t.IsActive).Id;

        await _stopJobTaskCommandHandler.HandleCommandAsync(new StopJobTask(jobTaskId));

        job = await this.WaitUntilJobAsync(jobId, job => !job.Tasks.Single(t => t.Id == jobTaskId).IsActive);
        job.Tasks.Single(t => t.Id == jobTaskId).State.ShouldBe(JobTaskState.Cancelled);
    }
}