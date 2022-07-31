using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.EntityFrameworkCore;

namespace Wilgysef.Stalk.WebApi.Tests;

public class WebApiFactory : IDisposable
{
    private DbConnection? _connection;

    public WebApplicationFactory<Program> CreateApplication()
    {
        if (_connection == null)
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            using var context = new StalkDbContext(SetOptions().Options);
            context.Database.EnsureCreated();
            context.Jobs.Add(Job.Create(1234));
            context.SaveChanges();
        }

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices((IServiceCollection services) =>
                {
                    services.AddDbContext<StalkDbContext>(options =>
                    {
                        SetOptions(options);
                    });
                });
            });
    }

    public void Dispose()
    {
        if (_connection != null)
        {
            _connection.Dispose();
            _connection = null;
        }
    }

    private DbContextOptionsBuilder<StalkDbContext> SetOptions(DbContextOptionsBuilder<StalkDbContext>? builder = null)
    {
        builder ??= new DbContextOptionsBuilder<StalkDbContext>();

        SetOptions((DbContextOptionsBuilder)builder);
        return builder;
    }

    private DbContextOptionsBuilder SetOptions(DbContextOptionsBuilder builder)
    {
        return builder
            .UseSqlite(_connection);
    }
}
