using IdGen;
using Wilgysef.Stalk.Application.Contracts.Commands.Jobs;
using Wilgysef.Stalk.Application.Contracts.Dtos;
using Wilgysef.Stalk.Core.JobWorkerManagers;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Shared.Cqrs;

namespace Wilgysef.Stalk.Application.Commands;

public class JobCommandHandler
    : ApplicationCommand,
    ICommandHandler<CreateJob, JobDto>,
    ICommandHandler<StopJob, JobDto>
{
    private readonly IJobManager _jobManager;
    private readonly IJobStateManager _jobStateManager;
    private readonly IJobWorkerManager _jobWorkerManager;
    private readonly IIdGenerator<long> _idGenerator;

    public JobCommandHandler(
        IJobManager jobManager,
        IJobStateManager jobStateManager,
        IJobWorkerManager jobWorkerManager,
        IIdGenerator<long> idGenerator)
    {
        _jobManager = jobManager;
        _jobStateManager = jobStateManager;
        _jobWorkerManager = jobWorkerManager;
        _idGenerator = idGenerator;
    }

    public async Task<JobDto> HandleCommandAsync(CreateJob command)
    {
        var job = await _jobManager.CreateJobAsync(Job.Create(
            _idGenerator.CreateId(),
            command.Name));

        _jobWorkerManager.StartJobWorker(job);

        return Mapper.Map<JobDto>(job);
    }

    public async Task<JobDto> HandleCommandAsync(StopJob command)
    {
        var job = await _jobManager.GetJobAsync(command.Id);

        await _jobStateManager.StopJobAsync(job);

        return Mapper.Map<JobDto>(job);
    }
}
