using Shouldly;
using Wilgysef.Stalk.Application.Shared.Dtos.JobAppService;
using Wilgysef.Stalk.Application.Shared.Services;
using Wilgysef.Stalk.Core.Shared.Enums;
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
        var input = new CreateJobInput
        {
            Name = "test",
        };

        var job = await _jobAppService.CreateJobAsync(input);

        job.Name.ShouldBe(input.Name);
        job.State.ShouldBe(JobState.Inactive);
        job.Priority.ShouldBe(0);
        job.Started.ShouldBeNull();
        job.Finished.ShouldBeNull();
        job.ConfigJson.ShouldBeNull();
        job.Tasks.ShouldBeEmpty();
    }
}
