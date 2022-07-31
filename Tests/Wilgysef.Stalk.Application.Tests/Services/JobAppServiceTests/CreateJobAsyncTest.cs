using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wilgysef.Stalk.Application.Shared.Dtos;
using Wilgysef.Stalk.Application.Shared.Services;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Application.Tests.Services.JobAppServiceTests;

public class CreateJobAsyncTest : BaseTest
{
    private readonly IJobAppService _jobAppService;

    public CreateJobAsyncTest()
    {
        _jobAppService = GetRequiredService<IJobAppService>();
    }

    [Fact]
    public async Task Create_Job()
    {
        await _jobAppService.CreateJobAsync(new CreateJobInput
        {
            Name = "test",
        });
    }
}
