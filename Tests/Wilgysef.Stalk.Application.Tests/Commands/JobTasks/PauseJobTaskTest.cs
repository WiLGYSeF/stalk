using Autofac;
using AutoMapper;
using Wilgysef.Stalk.Application.Contracts.Commands.Jobs;
using Wilgysef.Stalk.Application.Contracts.Commands.JobTasks;
using Wilgysef.Stalk.Application.Contracts.Dtos;
using Wilgysef.Stalk.Application.Tests.Utilities;
using Wilgysef.Stalk.Core.JobTaskWorkers;
using Wilgysef.Stalk.Core.Shared.Cqrs;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;
using Wilgysef.Stalk.TestBase;
using Wilgysef.Stalk.TestBase.Extensions;
using Wilgysef.Stalk.TestBase.Mocks;

namespace Wilgysef.Stalk.Application.Tests.Commands.JobTasks;

public class PauseJobTaskTest : BaseTest
{
    private readonly ICommandHandler<CreateJob, JobDto> _createJobCommandHandler;

    private readonly JobStarter _jobStarter;
    private readonly IMapper _mapper;

    public PauseJobTaskTest()
    {
        ReplaceSingletonService<IJobTaskWorkerFactory>(c => new JobTaskWorkerFactoryMock(
            c.Resolve<IServiceLocator>()));

        _createJobCommandHandler = GetRequiredService<ICommandHandler<CreateJob, JobDto>>();

        _jobStarter = new JobStarter(BeginLifetimeScope());
        _mapper = GetRequiredService<IMapper>();
    }

    [Fact]
    public async Task Pause_JobTask()
    {
        var createCommand = new CreateJobBuilder(_mapper).WithRandom().Create();

        var jobDto = await _createJobCommandHandler.HandleCommandAsync(createCommand);
        var jobId = long.Parse(jobDto.Id);

        await _jobStarter.WorkPrioritizedJobsAsync();

        var job = await this.WaitUntilJobAsync(jobId, job => job.Tasks.Any(t => t.IsActive));
        var jobTaskId = job.Tasks.First(t => t.IsActive).Id;

        using (var scope = BeginLifetimeScope())
        {
            var pauseJobTaskCommandHandler = scope.GetRequiredService<ICommandHandler<PauseJobTask, JobDto>>();
            await pauseJobTaskCommandHandler.HandleCommandAsync(new PauseJobTask(jobTaskId));
        }

        job = await this.WaitUntilJobAsync(jobId, job => job.Tasks.Single(t => t.Id == jobTaskId).State == JobTaskState.Paused);
    }
}
