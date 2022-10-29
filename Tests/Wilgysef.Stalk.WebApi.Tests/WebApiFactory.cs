using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wilgysef.Stalk.EntityFrameworkCore;

namespace Wilgysef.Stalk.WebApi.Tests;

public class WebApiFactory : IDisposable
{
    private readonly string _databaseName = Guid.NewGuid().ToString();
    private string DatabaseName => _databaseName;

    public WebApplicationFactory<Program> CreateApplication()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices((IServiceCollection services) =>
                {
                    services.AddDbContext<StalkDbContext>(options =>
                    {
                        // TODO: use MySql?
                        options.UseInMemoryDatabase(DatabaseName);
                    });
                });
            });
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
