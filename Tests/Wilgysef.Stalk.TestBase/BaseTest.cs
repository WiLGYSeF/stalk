using Autofac;
using Autofac.Builder;
using Autofac.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using System;
using System.Net;
using System.Text.RegularExpressions;
using Wilgysef.HttpClientInterception;
using Wilgysef.Stalk.Application.ServiceRegistrar;
using Wilgysef.Stalk.Core.Shared.FileServices;
using Wilgysef.Stalk.Core.Shared.Options;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;
using Wilgysef.Stalk.EntityFrameworkCore;
using Wilgysef.Stalk.TestBase.Mocks;

namespace Wilgysef.Stalk.TestBase;

public abstract class BaseTest
{
    public bool RegisterExtractors { get; set; } = false;

    public bool RegisterDownloaders { get; set; } = false;

    public bool DoMockFileService { get; set; } = true;

    public bool DoMockHttpClient { get; set; } = true;

    public MockFileService? MockFileService { get; private set; }

    public HttpClientInterceptor? HttpClientInterceptor { get; private set; }

    public HttpRequestEntryLog? HttpRequestEntryLog { get; private set; }

    private IServiceProvider? _serviceProvider;
    private IServiceProvider ServiceProvider
    {
        get => _serviceProvider ??= GetServiceProvider();
        set => _serviceProvider = value;
    }

    private readonly List<(Type Implementation, Type Service, ServiceRegistrationType RegistrationType)> _replaceServices = new();
    private readonly List<(object Implementation, Type Service)> _replaceServiceInstances = new();
    private readonly List<(Func<IComponentContext, object>, Type Service, ServiceRegistrationType RegistrationType)> _replaceServiceDelegates = new();

    private readonly string _databaseName = Guid.NewGuid().ToString();
    private string DatabaseName => _databaseName;

    #region Service Registration

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

    public void ReplaceServiceInstance<TImplementation, TService>(TImplementation instance)
        where TImplementation : class
        where TService : notnull
    {
        ReplaceServiceInstance(instance, typeof(TService));
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

    public void ReplaceServiceInstance<T>(T implementation) where T : class
    {
        _replaceServiceInstances.Add((implementation, typeof(T)));
        _serviceProvider = null;
    }

    public void ReplaceServiceInstance<T>(T implementation, Type service) where T : class
    {
        _replaceServiceInstances.Add((implementation, service));
        _serviceProvider = null;
    }

    public void ReplaceService<T>(Func<IComponentContext, T> @delegate) where T : class
    {
        ReplaceTransientService(@delegate);
    }

    public void ReplaceTransientService<T>(Func<IComponentContext, T> @delegate) where T : class
    {
        _replaceServiceDelegates.Add((@delegate, typeof(T), ServiceRegistrationType.Transient));
        _serviceProvider = null;
    }

    public void ReplaceScopedService<T>(Func<IComponentContext, T> @delegate) where T : class
    {
        _replaceServiceDelegates.Add((@delegate, typeof(T), ServiceRegistrationType.Scoped));
        _serviceProvider = null;
    }

    public void ReplaceSingletonService<T>(Func<IComponentContext, T> @delegate) where T : class
    {
        _replaceServiceDelegates.Add((@delegate, typeof(T), ServiceRegistrationType.Singleton));
        _serviceProvider = null;
    }

    private IServiceProvider GetServiceProvider(ContainerBuilder? builder = null)
    {
        return new AutofacServiceProviderFactory()
            .CreateServiceProvider(builder ?? CreateContainerBuilder(new ServiceCollection()));
    }

    private DbContextOptionsBuilder<StalkDbContext> GetDbContextOptionsBuilder()
    {
        return new DbContextOptionsBuilder<StalkDbContext>()
            .UseInMemoryDatabase(DatabaseName);
    }

    private ContainerBuilder CreateContainerBuilder(IServiceCollection services)
    {
        var builder = new ContainerBuilder();

        var serviceRegistrar = new ServiceRegistrar
        {
            RegisterExtractors = RegisterExtractors,
            RegisterDownloaders = RegisterDownloaders,
        };
        serviceRegistrar.RegisterServices(services);
        serviceRegistrar.RegisterDbContext(builder, GetDbContextOptionsBuilder().Options);

        builder.Populate(services);
        serviceRegistrar.RegisterServices(
            builder,
            new LoggerFactory(new[] { new DebugLoggerProvider() }).CreateLogger("test"),
            null,
            t => (Activator.CreateInstance(t) as IOptionSection)!);

        if (DoMockFileService)
        {
            ReplaceFileService();
        }
        if (DoMockHttpClient)
        {
            ReplaceHttpClient();
        }

        foreach (var (implementation, service, type) in _replaceServices)
        {
            var registration = builder.RegisterType(implementation)
                .As(service)
                .PropertiesAutowired();
            RegisterByType(registration, type);
        }

        foreach (var (implementation, service) in _replaceServiceInstances)
        {
            builder.RegisterInstance(implementation)
                .As(service)
                .PropertiesAutowired()
                .SingleInstance();
        }

        foreach (var (@delegate, service, type) in _replaceServiceDelegates)
        {
            var registration = builder.Register(@delegate)
                .As(service)
                .PropertiesAutowired();
            RegisterByType(registration, type);
        }

        return builder;

        static void RegisterByType<TLimit, TActivatorData, TRegistrationStyle>(IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle> registration, ServiceRegistrationType type)
        {
            _ = type switch
            {
                ServiceRegistrationType.Transient => registration.InstancePerDependency(),
                ServiceRegistrationType.Scoped => registration.InstancePerLifetimeScope(),
                ServiceRegistrationType.Singleton => registration.SingleInstance(),
                _ => throw new NotImplementedException(),
            };
        }
    }

    private enum ServiceRegistrationType
    {
        Transient,
        Scoped,
        Singleton,
    }

    #endregion

    #region Mocks

    private void ReplaceFileService()
    {
        MockFileService = new MockFileService();
        _replaceServiceInstances.Insert(0, (MockFileService, typeof(IFileService)));
    }

    private void ReplaceHttpClient()
    {
        HttpClientInterceptor = HttpClientInterceptor.Create()
            .AddForAny(request => new HttpResponseMessage(HttpStatusCode.NotFound), invokeEvents: true);
        HttpRequestEntryLog = new HttpRequestEntryLog();

        HttpClientInterceptor.RequestProcessed += (sender, request) =>
        {
            HttpRequestEntryLog.AddEntry(new HttpRequestEntry(request, DateTimeOffset.Now, null, null));
        };
        HttpClientInterceptor.ResponseReceived += (sender, response) =>
        {
            if (response.RequestMessage != null)
            {
                HttpRequestEntryLog.SetEntryResponse(response.RequestMessage, response, DateTimeOffset.Now);
            }
        };

        _replaceServiceDelegates.Insert(
            0,
            (c => new HttpClient(HttpClientInterceptor), typeof(HttpClient), ServiceRegistrationType.Transient));
    }

    #endregion

    #region Wait

    /// <summary>
    /// Waits until a condition is met.
    /// </summary>
    /// <param name="condition">Condition to meet, stops waiting when <see langword="true"/> is returned.</param>
    /// <param name="timeout">Wait timeout.</param>
    /// <returns><see langword="true"/> if the condition was met, <see langword="false"/> if the timeout occurred.</returns>
    public static bool WaitUntil(Func<bool> condition, TimeSpan timeout)
    {
        var spin = new SpinWait();
        var startTime = DateTime.Now;

        while (DateTime.Now - startTime < timeout)
        {
            if (condition())
            {
                return true;
            }
            spin.SpinOnce();
        }

        return condition();
    }

    /// <summary>
    /// Waits until a condition is met.
    /// </summary>
    /// <param name="condition">Condition to meet, stops waiting when <see langword="true"/> is returned.</param>
    /// <param name="timeout">Wait timeout.</param>
    /// <param name="interval">Interval between checking condition.</param>
    /// <returns><see langword="true"/> if the condition was met, <see langword="false"/> if the timeout occurred.</returns>
    public static bool WaitUntil(Func<bool> condition, TimeSpan timeout, TimeSpan interval)
    {
        var startTime = DateTime.Now;

        while (DateTime.Now - startTime < timeout)
        {
            if (condition())
            {
                return true;
            }
            Thread.Sleep(interval);
        }

        return condition();
    }

    /// <summary>
    /// Waits until a condition is met.
    /// </summary>
    /// <param name="condition">Condition to meet, stops waiting when <see langword="true"/> is returned.</param>
    /// <param name="timeout">Wait timeout.</param>
    /// <returns><see langword="true"/> if the condition was met, <see langword="false"/> if the timeout occurred.</returns>
    public static async Task<bool> WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout)
    {
        var spin = new SpinWait();
        var startTime = DateTime.Now;

        while (DateTime.Now - startTime < timeout)
        {
            if (await condition())
            {
                return true;
            }
            spin.SpinOnce();
        }

        return await condition();
    }

    /// <summary>
    /// Waits until a condition is met.
    /// </summary>
    /// <param name="condition">Condition to meet, stops waiting when <see langword="true"/> is returned.</param>
    /// <param name="timeout">Wait timeout.</param>
    /// <param name="interval">Interval between checking condition.</param>
    /// <returns><see langword="true"/> if the condition was met, <see langword="false"/> if the timeout occurred.</returns>
    public static async Task<bool> WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout, TimeSpan interval)
    {
        if (interval == TimeSpan.Zero)
        {
            return await WaitUntilAsync(condition, timeout);
        }

        var startTime = DateTime.Now;

        while (DateTime.Now - startTime < timeout)
        {
            if (await condition())
            {
                return true;
            }
            await Task.Delay(interval);
        }

        return await condition();
    }

    #endregion

    #region Scope

    /// <summary>
    /// Begins a lifetime scope.
    /// </summary>
    public IServiceLifetimeScope BeginLifetimeScope()
    {
        var serviceLocator = GetRequiredService<IServiceLocator>();
        return serviceLocator.BeginLifetimeScope();
    }

    #endregion
}
