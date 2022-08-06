using Microsoft.AspNetCore.Mvc;
using Wilgysef.Stalk.Application.Commands;
using Wilgysef.Stalk.Application.Contracts.Commands.Jobs;
using Wilgysef.Stalk.Application.Contracts.Dtos;
using Wilgysef.Stalk.Core.Shared.Cqrs;

namespace Wilgysef.Stalk.WebApi.Controllers;

[Route("api/job")]
[ApiController]
public class JobController : ControllerBase
{
    private readonly ICommandHandler<CreateJob, JobDto> _createJobCommandHandler;
    private readonly ICommandHandler<StopJob, JobDto> _stopJobCommandHandler;

    public JobController(
        ICommandHandler<CreateJob, JobDto> createJobCommandHandler,
        ICommandHandler<StopJob, JobDto> stopJobCommandHandler)
    {
        _createJobCommandHandler = createJobCommandHandler;
        _stopJobCommandHandler = stopJobCommandHandler;
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
}
