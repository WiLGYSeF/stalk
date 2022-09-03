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
    private readonly IJobTaskManager _jobTaskManager;
    private readonly IJobTaskStateManager _jobTaskStateManager;
    private readonly IIdGenerator<long> _idGenerator;

    public JobTaskCommandHandler(
        IJobManager jobManager,
        IJobTaskManager jobTaskManager,
        IJobTaskStateManager jobTaskStateManager,
        IIdGenerator<long> idGenerator)
    {
        _jobManager = jobManager;
        _jobTaskManager = jobTaskManager;
        _jobTaskStateManager = jobTaskStateManager;
        _idGenerator = idGenerator;
    }

    public async Task<JobDto> HandleCommandAsync(CreateJobTask command)
    {
        var job = await _jobManager.GetJobAsync(command.JobId);

        var builder = new JobTaskBuilder()
            .WithJob(job)
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
        var task = await _jobTaskManager.GetJobTaskAsync(command.Id);

        // TODO: do not await
        await _jobTaskStateManager.StopJobTaskAsync(task);

        return Mapper.Map<JobDto>(task.Job);
    }

    public async Task<JobDto> HandleCommandAsync(DeleteJobTask command)
    {
        var task = await _jobTaskManager.GetJobTaskAsync(command.Id);

        // TODO: do not await
        await _jobTaskStateManager.StopJobTaskAsync(task);
        await _jobTaskManager.DeleteJobTaskAsync(task);

        return Mapper.Map<JobDto>(task.Job);
    }

    public async Task<JobDto> HandleCommandAsync(PauseJobTask command)
    {
        var task = await _jobTaskManager.GetJobTaskAsync(command.Id);

        // TODO: do not await
        await _jobTaskStateManager.PauseJobTaskAsync(task);

        return Mapper.Map<JobDto>(task.Job);
    }

    public async Task<JobDto> HandleCommandAsync(UnpauseJobTask command)
    {
        var task = await _jobTaskManager.GetJobTaskAsync(command.Id);

        // TODO: do not await
        await _jobTaskStateManager.UnpauseJobTaskAsync(task);

        return Mapper.Map<JobDto>(task.Job);
    }
}
