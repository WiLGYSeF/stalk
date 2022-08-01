using IdGen;
using Microsoft.AspNetCore.Mvc;
using Wilgysef.Stalk.Application.Shared.Dtos.JobAppService;
using Wilgysef.Stalk.Application.Shared.Services;

namespace Wilgysef.Stalk.WebApi.Controllers;

[Route("api/job")]
[ApiController]
public class JobController : ControllerBase
{
    private readonly IJobAppService _jobAppService;
    private readonly IIdGenerator<long> _idGenerator;

    public JobController(
        IJobAppService jobAppService,
        IIdGenerator<long> idGenerator)
    {
        _jobAppService = jobAppService;
        _idGenerator = idGenerator;
    }

    [HttpPost]
    public async Task CreateJob(CreateJobInput input)
    {
        var a = _idGenerator.CreateId();
        await _jobAppService.CreateJobAsync(input);
    }
}
