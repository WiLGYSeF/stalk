using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Wilgysef.Stalk.Application;
using Wilgysef.Stalk.Application.ServiceRegistrar;
using Wilgysef.Stalk.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

ConfigureServices();
ConfigureSwagger();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// TODO: remove debug
var context = app.Services.GetRequiredService<IComponentContext>();
var services = context.ComponentRegistry.Registrations
    .Where(r => r.Services.Any(s => s.Description.StartsWith("Wilgysef")))
    .ToList();

using (var scope = app.Services.CreateScope())
{
    var appStartup = scope.ServiceProvider.GetRequiredService<Startup>();
    await appStartup.StartAsync();
}

app.Run();

void ConfigureServices()
{
    builder.Services.AddAutofac();

    builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

    builder.Host.ConfigureContainer<ContainerBuilder>((context, containerBuilder) =>
    {
        // TODO: create appsettings and move dev connectionstring

        var serviceRegistrar = new ServiceRegistrar(
            new DbContextOptionsBuilder<StalkDbContext>()
                .UseMySql("server=localhost;database=stalk;user=user;password=NlZbRHyeYXV6mIA;", new MySqlServerVersion(new Version(8, 0, 3)))
                .Options);
        serviceRegistrar.RegisterApplication(containerBuilder, builder.Services);
    });
}

void ConfigureSwagger()
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

public partial class Program { }
