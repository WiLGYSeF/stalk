using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutoMapper.Contrib.Autofac.DependencyInjection;
using IdGen;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;
using System.Reflection;
using Wilgysef.Stalk.Application;
using Wilgysef.Stalk.Application.ServiceRegistrar;
using Wilgysef.Stalk.Core;
using Wilgysef.Stalk.EntityFrameworkCore;

namespace Wilgysef.Stalk.TestBase;

public class BaseTest
{
    private static Type[] DependsOn = new[]
    {
        typeof(ApplicationModule),
        typeof(CoreModule),
    };

    private const int IdGeneratorId = 1;

    private IServiceProvider? _serviceProvider;
    private IServiceProvider ServiceProvider
    {
        get
        {
            if (_serviceProvider == null)
            {
                _serviceProvider = GetServiceProvider();
            }
            return _serviceProvider;
        }
        set => _serviceProvider = value;
    }

    private DbConnection? _connection;

    private List<(Type Implementation, Type Service, ServiceRegistrationType RegistrationType)> _replaceServices = new();

    public T? GetService<T>() where T : notnull
    {
        return ServiceProvider.GetService<T>();
    }

    public IEnumerable<T> GetServices<T>() where T : notnull
    {
        return ServiceProvider.GetServices<T>();
    }

    public T GetRequiredService<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    public void ReplaceService<TImplementation, TService>()
        where TImplementation : notnull
        where TService : notnull
    {
        ReplaceService(typeof(TImplementation), typeof(TService));
    }

    public void ReplaceTransientService<TImplementation, TService>()
        where TImplementation : notnull
        where TService : notnull
    {
        ReplaceTransientService(typeof(TImplementation), typeof(TService));
    }

    public void ReplaceScopedService<TImplementation, TService>()
        where TImplementation : notnull
        where TService : notnull
    {
        ReplaceScopedService(typeof(TImplementation), typeof(TService));
    }

    public void ReplaceSingletonService<TImplementation, TService>()
        where TImplementation : notnull
        where TService : notnull
    {
        ReplaceSingletonService(typeof(TImplementation), typeof(TService));
    }

    public void ReplaceService(Type implementation, Type service)
    {
        ReplaceTransientService(implementation, service);
    }

    public void ReplaceTransientService(Type implementation, Type service)
    {
        _replaceServices.Add((implementation, service, ServiceRegistrationType.Transient));
        _serviceProvider = null;
    }

    public void ReplaceScopedService(Type implementation, Type service)
    {
        _replaceServices.Add((implementation, service, ServiceRegistrationType.Scoped));
        _serviceProvider = null;
    }

    public void ReplaceSingletonService(Type implementation, Type service)
    {
        _replaceServices.Add((implementation, service, ServiceRegistrationType.Singleton));
        _serviceProvider = null;
    }

    private IServiceProvider GetServiceProvider(ContainerBuilder? builder = null)
    {
        return new AutofacServiceProviderFactory()
            .CreateServiceProvider(builder
                ?? CreateContainerBuilder(CreateServiceCollection()));
    }

    private ServiceCollection CreateServiceCollection()
    {
        var services = new ServiceCollection();

        if (_connection == null)
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            using var context = new StalkDbContext(new DbContextOptionsBuilder<StalkDbContext>()
                .UseSqlite(_connection)
                .Options);
            context.Database.EnsureCreated();
        }

        services.AddDbContext<StalkDbContext>(options =>
        {
            options.UseSqlite(_connection);
        });

        return services;
    }

    private ContainerBuilder CreateContainerBuilder(IServiceCollection? services = null)
    {
        var builder = new ContainerBuilder();

        if (services != null)
        {
            builder.Populate(services);
        }

        var serviceRegistrar = new ServiceRegistrar();
        serviceRegistrar.RegisterApplication(builder);

        foreach (var (implementation, service, type) in _replaceServices)
        {
            var registration = builder.RegisterType(implementation)
                .As(service)
                .PropertiesAutowired();

            _ = type switch
            {
                ServiceRegistrationType.Transient => registration.InstancePerDependency(),
                ServiceRegistrationType.Scoped => registration.InstancePerLifetimeScope(),
                ServiceRegistrationType.Singleton => registration.SingleInstance(),
                _ => throw new NotImplementedException(),
            };
        }

        return builder;
    }

    private enum ServiceRegistrationType
    {
        Transient,
        Scoped,
        Singleton,
    }
}