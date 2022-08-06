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
    private readonly IQueryHandler<GetJob, JobDto> _getJobCommandHandler;

    public JobController(
        ICommandHandler<CreateJob, JobDto> createJobCommandHandler,
        ICommandHandler<StopJob, JobDto> stopJobCommandHandler,
        IQueryHandler<GetJob, JobDto> getJobCommandHandler)
    {
        _createJobCommandHandler = createJobCommandHandler;
        _stopJobCommandHandler = stopJobCommandHandler;
        _getJobCommandHandler = getJobCommandHandler;
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

    [HttpGet]
    public async Task<JobDto> GetJobAsync(long id)
    {
        return await _getJobCommandHandler.HandleQueryAsync(new GetJob(id));
    }
}
