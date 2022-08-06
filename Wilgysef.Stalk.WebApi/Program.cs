using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Wilgysef.Stalk.Application.ServiceRegistrar;

using Wilgysef.Stalk.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

ConfigureDependencyInjection();
ConfigureDbContext();
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

var context = app.Services.GetRequiredService<IComponentContext>();

var services = context.ComponentRegistry.Registrations
    .Where(r => r.Services.Any(s => s.Description.StartsWith("Wilgysef")))
    .ToList();

app.Run();

void ConfigureDbContext()
{
    builder.Services.AddDbContext<StalkDbContext>(opt =>
    {
        opt.UseSqlite("DataSource=abc.db");
    });
}

void ConfigureSwagger()
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

void ConfigureDependencyInjection()
{
    builder.Services.AddAutofac();

    builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

    builder.Host.ConfigureContainer<ContainerBuilder>((context, builder) =>
    {
        var serviceRegistrar = new ServiceRegistrar();
        serviceRegistrar.RegisterApplication(builder);
    });
}

public partial class Program { }
