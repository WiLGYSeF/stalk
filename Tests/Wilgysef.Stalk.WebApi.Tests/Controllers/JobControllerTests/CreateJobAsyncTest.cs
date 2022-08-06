using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;

namespace Wilgysef.Stalk.WebApi.Tests.Controllers.JobControllerTests;

public class CreateJobAsyncTest
{
    private readonly WebApplicationFactory<Program> _factory;

    public CreateJobAsyncTest()
    {
        _factory = new WebApiFactory().CreateApplication();
    }

    [Fact]
    public async Task Create_Job()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/job", JsonContent.Create(new { }));

        var content = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
    }
}
