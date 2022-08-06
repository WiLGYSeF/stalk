using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutoMapper.Contrib.Autofac.DependencyInjection;
using IdGen;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Wilgysef.Stalk.Application;
using Wilgysef.Stalk.Core;
using Wilgysef.Stalk.Core.Shared;

using Wilgysef.Stalk.EntityFrameworkCore;

const int IdGeneratorId = 1;

// used for project reference so the assembly is loaded when registering dependency injection
// is there a better way to do this?
var DependsOn = new[]
{
    typeof(ApplicationModule),
    typeof(CoreModule),
};

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
        var assembly = Assembly.GetExecutingAssembly();

        builder.RegisterAutoMapper(true, ServiceRegistration.GetAssemblies(assembly).ToArray());

        builder.Register(c => new IdGenerator(IdGeneratorId, IdGeneratorOptions.Default))
            .As<IIdGenerator<long>>()
            .SingleInstance();

        foreach (var (implementation, service) in ServiceRegistration.GetTransientServiceImplementations(assembly))
        {
            builder.RegisterType(implementation)
                .As(service)
                .PropertiesAutowired()
                .InstancePerDependency();
        }

        foreach (var (implementation, service) in ServiceRegistration.GetScopedServiceImplementations(assembly))
        {
            builder.RegisterType(implementation)
                .As(service)
                .PropertiesAutowired()
                .InstancePerLifetimeScope();
        }

        foreach (var (implementation, service) in ServiceRegistration.GetSingletonServiceImplementations(assembly))
        {
            builder.RegisterType(implementation)
                .As(service)
                .PropertiesAutowired()
                .SingleInstance();
        }
    });
}

public partial class Program { }
