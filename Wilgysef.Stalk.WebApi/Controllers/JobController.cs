using Microsoft.AspNetCore.Mvc;
using Wilgysef.Stalk.Application.Contracts.Commands.Jobs;
using Wilgysef.Stalk.Application.Contracts.Dtos;
using Wilgysef.Stalk.Application.Contracts.Queries.Jobs;
using Wilgysef.Stalk.Core.Shared.Cqrs;

namespace Wilgysef.Stalk.WebApi.Controllers;

[Route("api/job")]
[ApiController]
public class JobController : ControllerBase
{
    private readonly ICommandHandler<CreateJob, JobDto> _createJobCommandHandler;
    private readonly ICommandHandler<StopJob, JobDto> _stopJobCommandHandler;
    private readonly ICommandHandler<DeleteJob, JobDto> _deleteJobCommandHandler;
    private readonly ICommandHandler<PauseJob, JobDto> _pauseJobCommandHandler;
    private readonly ICommandHandler<UnpauseJob, JobDto> _unpauseJobCommandHandler;
    private readonly IQueryHandler<GetJob, JobDto> _getJobCommandHandler;
    private readonly IQueryHandler<GetJobs, JobListDto> _getJobsCommandHandler;

    public JobController(
        ICommandHandler<CreateJob, JobDto> createJobCommandHandler,
        ICommandHandler<StopJob, JobDto> stopJobCommandHandler,
        ICommandHandler<DeleteJob, JobDto> deleteJobCommandHandler,
        ICommandHandler<PauseJob, JobDto> pauseJobCommandHandler,
        ICommandHandler<UnpauseJob, JobDto> unpauseJobCommandHandler,
        IQueryHandler<GetJob, JobDto> getJobCommandHandler,
        IQueryHandler<GetJobs, JobListDto> getJobsCommandHandler)
    {
        _createJobCommandHandler = createJobCommandHandler;
        _stopJobCommandHandler = stopJobCommandHandler;
        _deleteJobCommandHandler = deleteJobCommandHandler;
        _pauseJobCommandHandler = pauseJobCommandHandler;
        _unpauseJobCommandHandler = unpauseJobCommandHandler;
        _getJobCommandHandler = getJobCommandHandler;
        _getJobsCommandHandler = getJobsCommandHandler;
    }

    [HttpPost]
    public async Task<JobDto> CreateJobAsync(CreateJob command)
    {
        return await _createJobCommandHandler.HandleCommandAsync(command);
    }

    [HttpPost("{id}/stop")]
    public async Task<JobDto> StopJobAsync(long id)
    {
        return await _stopJobCommandHandler.HandleCommandAsync(new StopJob(id));
    }

    [HttpDelete("{id}")]
    public async Task<JobDto> DeleteJobAsync(long id)
    {
        return await _deleteJobCommandHandler.HandleCommandAsync(new DeleteJob(id));
    }

    [HttpPost("{id}/pause")]
    public async Task<JobDto> PauseJobAsync(long id)
    {
        return await _pauseJobCommandHandler.HandleCommandAsync(new PauseJob(id));
    }

    [HttpPost("{id}/unpause")]
    public async Task<JobDto> UnpauseJobAsync(long id)
    {
        return await _unpauseJobCommandHandler.HandleCommandAsync(new UnpauseJob(id));
    }

    [HttpGet]
    public async Task<JobDto> GetJobAsync(long id)
    {
        return await _getJobCommandHandler.HandleQueryAsync(new GetJob(id));
    }

    [HttpPost("list")]
    public async Task<JobListDto> GetJobsAsync(GetJobs query)
    {
        return await _getJobsCommandHandler.HandleQueryAsync(query);
    }
}
