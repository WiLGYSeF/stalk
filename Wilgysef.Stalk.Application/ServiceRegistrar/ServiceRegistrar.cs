using Autofac;
using Autofac.Builder;
using Autofac.Extensions.DependencyInjection;
using Autofac.Features.Scanning;
using AutoMapper.Contrib.Autofac.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using System.Reflection;
using Wilgysef.Stalk.Application.AssemblyLoaders;
using Wilgysef.Stalk.Application.HttpClientPolicies;
using Wilgysef.Stalk.Application.IdGenerators;
using Wilgysef.Stalk.Core;
using Wilgysef.Stalk.Core.Shared;
using Wilgysef.Stalk.Core.Shared.Cqrs;
using Wilgysef.Stalk.Core.Shared.Dependencies;
using Wilgysef.Stalk.Core.Shared.Downloaders;
using Wilgysef.Stalk.Core.Shared.Extractors;
using Wilgysef.Stalk.Core.Shared.IdGenerators;
using Wilgysef.Stalk.Core.Shared.Options;
using Wilgysef.Stalk.EntityFrameworkCore;

namespace Wilgysef.Stalk.Application.ServiceRegistrar;

public class ServiceRegistrar
{
    // used for project reference so the assembly is loaded when registering dependency injection
    // is there a better way to do this?
    private static readonly Type[] DependsOn = new[]
    {
        typeof(ApplicationModule),
        typeof(CoreModule),
        typeof(EntityFrameworkCoreModule),
    };

    public bool RegisterExtractors { get; set; } = true;

    public bool RegisterDownloaders { get; set; } = true;

    public int IdGeneratorId { get; set; } = 1;

    public DbContextOptions<StalkDbContext> DbContextOptions { get; set; }

    public ILogger Logger { get; set; }

    public string ExternalAssembliesPath { get; set; }

    public Func<Type, IOptionSection> GetConfig { get; set; }

    public ServiceRegistrar(
        DbContextOptions<StalkDbContext> options,
        ILogger logger,
        string externalAssembliesPath,
        Func<Type, IOptionSection> getConfig)
    {
        DbContextOptions = options;
        Logger = logger;
        ExternalAssembliesPath = externalAssembliesPath;
        GetConfig = getConfig;
    }

    /// <summary>
    /// Register application dependencies.
    /// </summary>
    /// <param name="builder">Container builder.</param>
    public void RegisterApplication(ContainerBuilder builder, IServiceCollection services)
    {
        var assemblies = GetAssemblies(Assembly.GetExecutingAssembly(), EligibleAssemblyFilter).ToList();
        var externalAssemblies = AssemblyLoader.LoadAssemblies(ExternalAssembliesPath);

        var loadedAssemblies = ToArray(
            assemblies.Count + externalAssemblies.Count,
            assemblies.Concat(externalAssemblies));

        // Polly registration
        services.AddHttpClient(Constants.HttpClientExtractorDownloaderName)
            .AddExtractorDownloaderClientPolicy();

        // TODO: fix
        //builder.Populate(services);

        // HttpClient registration
        builder.Register(c => c.Resolve<IHttpClientFactory>().CreateClient())
            .As<HttpClient>();

        builder.RegisterAutoMapper(true, loadedAssemblies);

        // IdGen registration
        builder.Register(c => new IdGenerator(new IdGen.IdGenerator(IdGeneratorId, IdGen.IdGeneratorOptions.Default)))
            .As<IIdGenerator<long>>()
            .SingleInstance();

        // Quartz registration
        builder.Register(c => new StdSchedulerFactory())
            .As<ISchedulerFactory>()
            .SingleInstance();
        RegisterAssemblyTypes<IJob>(builder, loadedAssemblies)
            .AsSelf()
            .InstancePerDependency();

        // WebApi tests add DbContext for testing
        if (!(services?.Any(s => s.ServiceType == typeof(StalkDbContext)) ?? false))
        {
            builder.Register(c => DbContextOptions);
            builder.RegisterType<StalkDbContext>()
                // WithParameter is broken?
                //.WithParameter("options", DbContextOptions)
                .As<IStalkDbContext>()
                .As<StalkDbContext>()
                .InstancePerLifetimeScope();
        }

        if (Logger != null)
        {
            builder.Register(c => Logger)
                .As<ILogger>()
                .SingleInstance();
        }

        RegisterAssemblyTypes<ITransientDependency>(builder, loadedAssemblies)
            .InstancePerDependency();
        RegisterAssemblyTypes<IScopedDependency>(builder, loadedAssemblies)
            .InstancePerLifetimeScope();
        RegisterAssemblyTypes<ISingletonDependency>(builder, loadedAssemblies)
            .SingleInstance();

        RegisterAssemblyTypes(typeof(ICommandHandler<,>), builder, loadedAssemblies)
            .InstancePerDependency();
        RegisterAssemblyTypes(typeof(IQueryHandler<,>), builder, loadedAssemblies)
            .InstancePerDependency();

        //var classes = loadedAssemblies
        //    .SelectMany(a => a.GetTypes())
        //    .Where(t => t.GetInterfaces().Contains(typeof(IOptionSection)) && t.IsClass);
        //foreach (var @class in classes)
        //{
        //    builder.Register(c => Convert.ChangeType(GetConfig(@class), @class))
        //        .As(@class)
        //        .InstancePerDependency();
        //}

        RegisterAssemblyTypes<IOptionSection>(builder, loadedAssemblies)
            .InstancePerDependency();

        if (RegisterExtractors)
        {
            RegisterAssemblyTypes<IExtractor>(builder, loadedAssemblies)
                .InstancePerDependency();
        }

        if (RegisterDownloaders)
        {
            RegisterAssemblyTypes<IDownloader>(builder, loadedAssemblies)
                .InstancePerDependency();
        }

        builder.RegisterType<Startup>()
            .AsSelf()
            .SingleInstance();
    }

    private IRegistrationBuilder<object, ScanningActivatorData, DynamicRegistrationStyle> RegisterAssemblyTypes(
        Type type,
        ContainerBuilder builder,
        params Assembly[] assemblies)
    {
        return builder.RegisterAssemblyTypes(assemblies)
            .Where(t => t.GetInterfaces().Any(i => i.IsAssignableFrom(type)))
            .AsImplementedInterfaces()
            .PropertiesAutowired();
    }

    private IRegistrationBuilder<object, ScanningActivatorData, DynamicRegistrationStyle> RegisterAssemblyTypes<T>(
        ContainerBuilder builder,
        params Assembly[] assemblies)
    {
        return RegisterAssemblyTypes(typeof(T), builder, assemblies);
    }

    private bool EligibleAssemblyFilter(Assembly assembly) => assembly.FullName != null && assembly.FullName.StartsWith("Wilgysef");

    private IEnumerable<Assembly> GetAssemblies(Assembly assembly, Func<Assembly, bool>? filter = null)
    {
        var stack = new Stack<Assembly>();
        var assembyNames = new HashSet<string>();

        stack.Push(assembly);

        while (stack.Count > 0)
        {
            var asm = stack.Pop();
            if (filter != null && !filter(asm))
            {
                continue;
            }

            yield return asm;

            foreach (var reference in asm.GetReferencedAssemblies())
            {
                if (!assembyNames.Contains(reference.FullName))
                {
                    stack.Push(Assembly.Load(reference));
                    assembyNames.Add(reference.FullName);
                }
            }
        }
    }

    private T[] ToArray<T>(int count, IEnumerable<T> items)
    {
        var arr = new T[count];
        var index = 0;

        foreach (var item in items)
        {
            arr[index++] = item;
        }

        return arr;
    }
}
