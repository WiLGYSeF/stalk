using Autofac;
using Autofac.Builder;
using Autofac.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using System.Diagnostics;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
using Wilgysef.HttpClientInterception;
using Wilgysef.Stalk.Application.ServiceRegistrar;
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

    public MockFileSystem? MockFileSystem { get; private set; }

    public HttpClientInterceptor? HttpClientInterceptor { get; private set; }

    public HttpRequestEntryLog? HttpRequestEntryLog { get; private set; }

    private IServiceProvider ServiceProvider
    {
        get => _serviceProvider ??= GetServiceProvider();
        set => _serviceProvider = value;
    }
    private IServiceProvider? _serviceProvider;

    private string DatabaseName => _databaseName;
    private readonly string _databaseName = Guid.NewGuid().ToString();

    private readonly List<RegistrationReplace> _replaceServices = new();

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

    #region Replace Services

    public void ReplaceService<TService, TImplementation>()
        where TService : class
        where TImplementation : class
        => ReplaceService(typeof(TService), typeof(TImplementation));

    public void ReplaceTransientService<TService, TImplementation>()
        where TService : class
        where TImplementation : class
        => ReplaceTransientService(typeof(TService), typeof(TImplementation));

    public void ReplaceScopedService<TService, TImplementation>()
        where TService : class
        where TImplementation : class
        => ReplaceScopedService(typeof(TService), typeof(TImplementation));

    public void ReplaceSingletonService<TService, TImplementation>()
        where TService : class
        where TImplementation : class
        => ReplaceSingletonService(typeof(TService), typeof(TImplementation));

    public void ReplaceService(Type service, Type implementation)
        => ReplaceTransientService(service, implementation);

    public void ReplaceTransientService(Type service, Type implementation)
        => AddRegistrationReplace(RegistrationReplace.Transient(service, implementation));

    public void ReplaceScopedService(Type service, Type implementation)
        => AddRegistrationReplace(RegistrationReplace.Scoped(service, implementation));

    public void ReplaceSingletonService(Type service, Type implementation)
        => AddRegistrationReplace(RegistrationReplace.Singleton(service, implementation));

    public void ReplaceService<T>(Func<IComponentContext, T> @delegate) where T : class
        => ReplaceTransientService(@delegate);

    public void ReplaceTransientService<T>(Func<IComponentContext, T> factory) where T : class
        => AddRegistrationReplace(RegistrationReplace.Transient(typeof(T), factory));

    public void ReplaceScopedService<T>(Func<IComponentContext, T> factory) where T : class
        => AddRegistrationReplace(RegistrationReplace.Scoped(typeof(T), factory));

    public void ReplaceSingletonService<T>(Func<IComponentContext, T> factory) where T : class
        => AddRegistrationReplace(RegistrationReplace.Singleton(typeof(T), factory));

    private void AddRegistrationReplace(RegistrationReplace replace)
    {
        _replaceServices.Add(replace);
        _serviceProvider = null;
    }

    #endregion

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

        ReplaceSingletonService<Core.Shared.Loggers.ILoggerFactory, LoggerFactoryMock>();

        if (DoMockFileService)
        {
            ReplaceFileService();
        }
        if (DoMockHttpClient)
        {
            ReplaceHttpClient();
        }

        foreach (var replace in _replaceServices)
        {
            replace.Register(builder);
        }

        return builder;
    }

    private class RegistrationReplace
    {
        public ServiceRegistrationType RegistrationType { get; }

        public Type ServiceType { get; }

        public Type? ImplementationType { get; }

        public Func<IComponentContext, object>? Factory { get; }

        private RegistrationReplace(ServiceRegistrationType type, Type serviceType, Type implementationType)
        {
            RegistrationType = type;
            ServiceType = serviceType;
            ImplementationType = implementationType;
        }

        private RegistrationReplace(ServiceRegistrationType type, Type serviceType, Func<IComponentContext, object> factory)
        {
            RegistrationType = type;
            ServiceType = serviceType;
            Factory = factory;
        }

        public void Register(ContainerBuilder builder)
        {
            if (Factory != null)
            {
                RegisterByType(RegisterFactory(builder));
            }
            else
            {
                RegisterByType(RegisterType(builder));
            }
        }

        private IRegistrationBuilder<object, SimpleActivatorData, SingleRegistrationStyle> RegisterFactory(ContainerBuilder builder)
        {
            return builder.Register(Factory!).As(ServiceType).PropertiesAutowired();
        }

        private IRegistrationBuilder<object, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterType(ContainerBuilder builder)
        {
            return builder.RegisterType(ImplementationType!).As(ServiceType).PropertiesAutowired();
        }

        private void RegisterByType<TLimit, TActivatorData, TRegistrationStyle>(IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle> registration)
        {
            _ = RegistrationType switch
            {
                ServiceRegistrationType.Transient => registration.InstancePerDependency(),
                ServiceRegistrationType.Scoped => registration.InstancePerLifetimeScope(),
                ServiceRegistrationType.Singleton => registration.SingleInstance(),
                _ => throw new InvalidOperationException(),
            };
        }

        public static RegistrationReplace Transient<TService, TImplementation>() => Transient(typeof(TService), typeof(TImplementation));

        public static RegistrationReplace Transient(Type serviceType, Type implementationType)
            => new(ServiceRegistrationType.Transient, serviceType, implementationType);

        public static RegistrationReplace Scoped<TService, TImplementation>() => Scoped(typeof(TService), typeof(TImplementation));

        public static RegistrationReplace Scoped(Type serviceType, Type implementationType)
            => new(ServiceRegistrationType.Scoped, serviceType, implementationType);

        public static RegistrationReplace Singleton<TService, TImplementation>() => Singleton(typeof(TService), typeof(TImplementation));

        public static RegistrationReplace Singleton(Type serviceType, Type implementationType)
            => new(ServiceRegistrationType.Singleton, serviceType, implementationType);

        public static RegistrationReplace Transient<TService>(Func<IComponentContext, object> factory) => Transient(typeof(TService), factory);

        public static RegistrationReplace Transient(Type serviceType, Func<IComponentContext, object> factory)
            => new(ServiceRegistrationType.Transient, serviceType, factory);

        public static RegistrationReplace Scoped<TService>(Func<IComponentContext, object> factory) => Scoped(typeof(TService), factory);

        public static RegistrationReplace Scoped(Type serviceType, Func<IComponentContext, object> factory)
            => new(ServiceRegistrationType.Scoped, serviceType, factory);

        public static RegistrationReplace Singleton<TService>(Func<IComponentContext, object> factory) => Singleton(typeof(TService), factory);

        public static RegistrationReplace Singleton(Type serviceType, Func<IComponentContext, object> factory)
            => new(ServiceRegistrationType.Singleton, serviceType, factory);
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
        MockFileSystem = new MockFileSystem();
        _replaceServices.Insert(0, RegistrationReplace.Singleton<IFileSystem>(_ => MockFileSystem));
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

        _replaceServices.Insert(0, RegistrationReplace.Transient<HttpClient>(_ => new HttpClient(HttpClientInterceptor)));
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
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
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
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
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
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
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

        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
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
