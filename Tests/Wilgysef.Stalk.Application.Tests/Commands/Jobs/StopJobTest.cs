﻿using Autofac;
using AutoMapper;
using Shouldly;
using Wilgysef.Stalk.Application.Contracts.Commands.Jobs;
using Wilgysef.Stalk.Application.Contracts.Dtos;
using Wilgysef.Stalk.Core.BackgroundJobs.Args;
using Wilgysef.Stalk.Core.BackgroundJobs.Executors;
using Wilgysef.Stalk.Core.JobTaskWorkerFactories;
using Wilgysef.Stalk.Core.JobWorkerServices;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Cqrs;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;
using Wilgysef.Stalk.TestBase;
using Wilgysef.Stalk.TestBase.Extensions;
using Wilgysef.Stalk.TestBase.Mocks;

namespace Wilgysef.Stalk.Application.Tests.Commands.Jobs;

public class StopJobTest : BaseTest
{
    private readonly JobTaskWorkerFactoryMock _jobTaskWorkerFactory;
    private readonly ICommandHandler<CreateJob, JobDto> _createJobCommandHandler;
    private readonly ICommandHandler<StopJob, JobDto> _stopJobCommandHandler;
    private readonly WorkPrioritizedJobsJob _workPrioritizedJobsJob;

    private readonly IMapper _mapper;

    public StopJobTest()
    {
        ReplaceSingletonService<IJobTaskWorkerFactory>(c => new JobTaskWorkerFactoryMock(
            c.Resolve<IServiceLocator>()));

        _jobTaskWorkerFactory = (JobTaskWorkerFactoryMock)GetRequiredService<IJobTaskWorkerFactory>();

        _createJobCommandHandler = GetRequiredService<ICommandHandler<CreateJob, JobDto>>();
        _stopJobCommandHandler = GetRequiredService<ICommandHandler<StopJob, JobDto>>();
        _workPrioritizedJobsJob = new WorkPrioritizedJobsJob(
            GetRequiredService<IJobManager>(),
            GetRequiredService<IJobWorkerService>());

        _mapper = GetRequiredService<IMapper>();
    }

    [Fact]
    public async Task Stop_Job()
    {
        // TODO: unstable test

        var createCommand = new CreateJobBuilder(_mapper).WithRandom().Create();

        var jobDto = await _createJobCommandHandler.HandleCommandAsync(createCommand);
        var jobId = long.Parse(jobDto.Id);

        await _workPrioritizedJobsJob.ExecuteJobAsync(new WorkPrioritizedJobsArgs());

        var job = await this.WaitUntilJobAsync(jobId, job => job.IsActive);
        job.State.ShouldBe(JobState.Active);

        await _stopJobCommandHandler.HandleCommandAsync(new StopJob(jobId));

        job = await this.WaitUntilJobAsync(jobId, job => job.IsDone);
        job.State.ShouldBe(JobState.Cancelled);
    }
}
