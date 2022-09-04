using Wilgysef.Stalk.Application.Contracts.Commands.Jobs;
using Wilgysef.Stalk.Application.Contracts.Dtos;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;
using Wilgysef.Stalk.Core.Shared.Cqrs;
using Wilgysef.Stalk.Core.Shared.IdGenerators;

namespace Wilgysef.Stalk.Application.Commands;

public class JobCommandHandler : Command,
    ICommandHandler<CreateJob, JobDto>,
    ICommandHandler<StopJob, JobDto>,
    ICommandHandler<DeleteJob, JobDto>,
    ICommandHandler<PauseJob, JobDto>,
    ICommandHandler<UnpauseJob, JobDto>
{
    private readonly IJobManager _jobManager;
    private readonly IJobStateManager _jobStateManager;
    private readonly IIdGenerator<long> _idGenerator;

    public JobCommandHandler(
        IJobManager jobManager,
        IJobStateManager jobStateManager,
        IIdGenerator<long> idGenerator)
    {
        _jobManager = jobManager;
        _jobStateManager = jobStateManager;
        _idGenerator = idGenerator;
    }

    public async Task<JobDto> HandleCommandAsync(CreateJob command)
    {
        var builder = new JobBuilder().WithId(_idGenerator.CreateId())
            .WithName(command.Name)
            .WithPriority(command.Priority)
            .WithDelayedUntilTime(command.DelayedUntil)
            .WithConfig(Mapper.Map<JobConfig>(command.Config));

        foreach (var task in command.Tasks)
        {
            var taskBuilder = new JobTaskBuilder().WithId(_idGenerator.CreateId())
                .WithName(task.Name)
                .WithPriority(task.Priority)
                .WithUri(task.Uri)
                .WithDelayedUntilTime(task.DelayedUntil);
            builder.WithTasks(taskBuilder.Create());
        }

        var job = await _jobManager.CreateJobAsync(builder.Create());

        return Mapper.Map<JobDto>(job);
    }

    public async Task<JobDto> HandleCommandAsync(StopJob command)
    {
        var job = await _jobManager.GetJobAsync(command.Id);

        // TODO: do not await
        await _jobStateManager.StopJobAsync(job);

        return Mapper.Map<JobDto>(job);
    }

    public async Task<JobDto> HandleCommandAsync(DeleteJob command)
    {
        var job = await _jobManager.GetJobAsync(command.Id);

        // TODO: do not await
        await _jobStateManager.StopJobAsync(job);
        await _jobManager.DeleteJobAsync(job);

        return Mapper.Map<JobDto>(job);
    }

    public async Task<JobDto> HandleCommandAsync(PauseJob command)
    {
        var job = await _jobManager.GetJobAsync(command.Id);

        // TODO: do not await
        await _jobStateManager.PauseJobAsync(job);

        return Mapper.Map<JobDto>(job);
    }

    public async Task<JobDto> HandleCommandAsync(UnpauseJob command)
    {
        var job = await _jobManager.GetJobAsync(command.Id);

        await _jobStateManager.UnpauseJobAsync(job);

        return Mapper.Map<JobDto>(job);
    }
}
