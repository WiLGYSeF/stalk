using Microsoft.AspNetCore.Mvc.Testing;

namespace Wilgysef.Stalk.WebApi.Tests;

public abstract class WebApiBaseTest
{
    public WebApplicationFactory<Program> WebApplicationFactory { get; }
    public HttpClient Client { get; }

    public WebApiBaseTest()
    {
        WebApplicationFactory = new WebApiFactory().CreateApplication();
        Client = WebApplicationFactory.CreateClient();
    }
}
