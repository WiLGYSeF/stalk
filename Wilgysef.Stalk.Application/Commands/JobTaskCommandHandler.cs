using IdGen;
using Wilgysef.Stalk.Application.Contracts.Commands.JobTasks;
using Wilgysef.Stalk.Application.Contracts.Dtos;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Cqrs;

namespace Wilgysef.Stalk.Application.Commands;

public class JobTaskCommandHandler : CommandQuery,
    ICommandHandler<CreateJobTask, JobDto>,
    ICommandHandler<StopJobTask, JobDto>,
    ICommandHandler<DeleteJobTask, JobDto>,
    ICommandHandler<PauseJobTask, JobDto>,
    ICommandHandler<UnpauseJobTask, JobDto>
{
    private readonly IJobManager _jobManager;
    private readonly IJobStateManager _jobStateManager;
    private readonly IIdGenerator<long> _idGenerator;

    public JobTaskCommandHandler(
        IJobManager jobManager,
        IJobStateManager jobStateManager,
        IIdGenerator<long> idGenerator)
    {
        _jobManager = jobManager;
        _jobStateManager = jobStateManager;
        _idGenerator = idGenerator;
    }

    public async Task<JobDto> HandleCommandAsync(CreateJobTask command)
    {
        var job = await _jobManager.GetJobAsync(command.JobId);

        var builder = new JobTaskBuilder()
            .WithId(_idGenerator.CreateId())
            .WithName(command.Name)
            .WithPriority(command.Priority)
            .WithUri(command.Uri)
            .WithDelayedUntilTime(command.DelayedUntil);

        job.AddTask(builder.Create());

        await _jobManager.UpdateJobAsync(job);

        return Mapper.Map<JobDto>(job);
    }

    public async Task<JobDto> HandleCommandAsync(StopJobTask command)
    {
        var job = await _jobManager.GetJobByTaskIdAsync(command.Id);
        var task = job.Tasks.Single(t => t.Id == command.Id);

        // TODO: do not await
        await _jobStateManager.StopJobTaskAsync(job, task);

        return Mapper.Map<JobDto>(job);
    }

    public async Task<JobDto> HandleCommandAsync(DeleteJobTask command)
    {
        var job = await _jobManager.GetJobByTaskIdAsync(command.Id);
        var task = job.Tasks.Single(t => t.Id == command.Id);

        // TODO: do not await
        await _jobStateManager.StopJobTaskAsync(job, task);

        job.RemoveTask(task);
        await _jobManager.UpdateJobAsync(job);

        return Mapper.Map<JobDto>(job);
    }

    public async Task<JobDto> HandleCommandAsync(PauseJobTask command)
    {
        var job = await _jobManager.GetJobByTaskIdAsync(command.Id);
        var task = job.Tasks.Single(t => t.Id == command.Id);

        // TODO: do not await
        await _jobStateManager.PauseJobTaskAsync(job, task);

        return Mapper.Map<JobDto>(job);
    }

    public async Task<JobDto> HandleCommandAsync(UnpauseJobTask command)
    {
        var job = await _jobManager.GetJobByTaskIdAsync(command.Id);
        var task = job.Tasks.Single(t => t.Id == command.Id);

        // TODO: do not await
        await _jobStateManager.UnpauseJobTaskAsync(job, task);

        return Mapper.Map<JobDto>(job);
    }
}
