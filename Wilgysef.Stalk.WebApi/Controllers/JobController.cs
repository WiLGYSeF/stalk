using Microsoft.AspNetCore.Mvc;
using Wilgysef.Stalk.Application.Shared.Dtos;
using Wilgysef.Stalk.Application.Shared.Dtos.JobAppService;
using Wilgysef.Stalk.Application.Shared.Services;

namespace Wilgysef.Stalk.WebApi.Controllers;

[Route("api/job")]
[ApiController]
public class JobController : ControllerBase
{
    private readonly IJobAppService _jobAppService;

    public JobController(
        IJobAppService jobAppService)
    {
        _jobAppService = jobAppService;
    }

    [HttpPost]
    public async Task<JobDto> CreateJobAsync(CreateJobInput input)
    {
        return await _jobAppService.CreateJobAsync(input);
    }

    [HttpPost("{id}/stop")]
    public async Task<JobDto> StopJobAsync(long id, StopJobInput input)
    {
        input.Id = id;
        return await _jobAppService.StopJobAsync(input);
    }
}
