using Microsoft.AspNetCore.Mvc;
using Wilgysef.Stalk.Core.Shared.Extractors;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.WebApi.Controllers;

[Route("api/extractor")]
[ApiController]
public class ExtractorController : ControllerBase
{
    private readonly IServiceLocator _serviceLocator;

    public ExtractorController(
        IServiceLocator serviceLocator)
    {
        _serviceLocator = serviceLocator;
    }

    [HttpGet("list")]
    public Task<object> GetExtractorsAsync()
    {
        var extractors = _serviceLocator.GetRequiredService<IEnumerable<IExtractor>>();

        return Task.FromResult<object>(new
        {
            Extractors = extractors.Select(e => new
            {
                e.Name,
            }).ToList(),
        });
    }
}
