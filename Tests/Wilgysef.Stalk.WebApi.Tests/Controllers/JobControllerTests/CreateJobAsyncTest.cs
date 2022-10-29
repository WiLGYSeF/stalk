using Shouldly;
using System.Net.Http.Json;
using Wilgysef.Stalk.Application.Contracts.Commands.Jobs;
using Wilgysef.Stalk.Application.Contracts.Dtos;

namespace Wilgysef.Stalk.WebApi.Tests.Controllers.JobControllerTests;

public class CreateJobAsyncTest : WebApiBaseTest
{
    [Fact]
    public async Task Create_Job()
    {
        var response = await Client.PostAsync("/api/job", JsonContent.Create(new CreateJob(
            null,
            new JobConfigDto(),
            Array.Empty<CreateJobTaskDto>())));

        var result = await response.EnsureSuccessAndDeserializeContent<JobDto>();
        result.ShouldNotBeNull();
    }
}
