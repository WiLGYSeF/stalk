using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using System.Net.Http.Json;
using Wilgysef.Stalk.Application.Contracts.Commands.Jobs;
using Wilgysef.Stalk.Application.Contracts.Dtos;

namespace Wilgysef.Stalk.WebApi.Tests.Controllers.JobControllerTests;

public class CreateJobAsyncTest
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CreateJobAsyncTest()
    {
        _factory = new WebApiFactory().CreateApplication();
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Create_Job()
    {
        var response = await _client.PostAsync("/api/job", JsonContent.Create(new CreateJob(
            null,
            new JobConfigDto(),
            Array.Empty<CreateJobTaskDto>())));

        var result = await response.EnsureSuccessAndDeserializeContent<JobDto>();
        result.ShouldNotBeNull();
    }
}
