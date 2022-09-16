using Microsoft.AspNetCore.Mvc;
using Wilgysef.Stalk.Core.Shared.Extractors;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.WebApi.Controllers;

[Route("api/extractor")]
[ApiController]
public class ExtractorController : ControllerBase
{
    private readonly IEnumerable<IExtractor> _extractors;

    public ExtractorController(
        IEnumerable<IExtractor> extractors)
    {
        _extractors = extractors;
    }

    [HttpGet("list")]
    public async Task<object> GetExtractorsAsync()
    {
        return new
        {
            Extractors = _extractors.Select(e => new
            {
                e.Name,
            }).ToList(),
        };
    }
}
