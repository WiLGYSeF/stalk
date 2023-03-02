using Microsoft.AspNetCore.Mvc;
using Wilgysef.Stalk.WebApi.Dtos;

namespace Wilgysef.Stalk.WebApi.Controllers;

[Route("api")]
[ApiController]
public class VersionController : ControllerBase
{
    [HttpGet("version")]
    public Task<VersionDto> GetExtractorsAsync()
    {
        return Task.FromResult(new VersionDto
        {
            Version = "20230301",
        });
    }
}
