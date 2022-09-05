using Autofac;
using Autofac.Builder;
using Autofac.Extensions.DependencyInjection;
using Autofac.Features.Scanning;
using AutoMapper.Contrib.Autofac.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Wilgysef.Stalk.Application.HttpClientPolicies;
using Wilgysef.Stalk.Application.IdGenerators;
using Wilgysef.Stalk.Core;
using Wilgysef.Stalk.Core.Shared;
using Wilgysef.Stalk.Core.Shared.Cqrs;
using Wilgysef.Stalk.Core.Shared.Dependencies;
using Wilgysef.Stalk.Core.Shared.Downloaders;
using Wilgysef.Stalk.Core.Shared.Extractors;
using Wilgysef.Stalk.Core.Shared.IdGenerators;
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

    public ServiceRegistrar(
        DbContextOptions<StalkDbContext> options,
        ILogger logger)
    {
        DbContextOptions = options;
        Logger = logger;
    }

    /// <summary>
    /// Register application dependencies.
    /// </summary>
    /// <param name="builder">Container builder.</param>
    public void RegisterApplication(ContainerBuilder builder, IServiceCollection services)
    {
        var assemblies = GetAssemblies(Assembly.GetExecutingAssembly(), EligibleAssemblyFilter)
            .ToList().ToArray();

        services.AddHttpClient(Constants.HttpClientExtractorDownloaderName)
            .AddExtractorDownloaderClientPolicy();

        // TODO: fix
        //builder.Populate(services);

        builder.Register(c => c.Resolve<IHttpClientFactory>().CreateClient())
            .As<HttpClient>();

        builder.RegisterAutoMapper(true, assemblies);

        builder.Register(c => new IdGenerator(new IdGen.IdGenerator(IdGeneratorId, IdGen.IdGeneratorOptions.Default)))
            .As<IIdGenerator<long>>()
            .SingleInstance();

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

        RegisterAssemblyTypes<ITransientDependency>(builder, assemblies)
            .InstancePerDependency();
        RegisterAssemblyTypes<IScopedDependency>(builder, assemblies)
            .InstancePerLifetimeScope();
        RegisterAssemblyTypes<ISingletonDependency>(builder, assemblies)
            .SingleInstance();

        RegisterAssemblyTypes(typeof(ICommandHandler<,>), builder, assemblies)
            .InstancePerDependency();
        RegisterAssemblyTypes(typeof(IQueryHandler<,>), builder, assemblies)
            .InstancePerDependency();

        if (RegisterExtractors)
        {
            RegisterAssemblyTypes(typeof(IExtractor), builder, assemblies)
                .InstancePerDependency();
        }

        if (RegisterDownloaders)
        {
            RegisterAssemblyTypes(typeof(IDownloader), builder, assemblies)
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

    private IEnumerable<Assembly> GetAssemblies(Assembly assembly, Func<Assembly, bool> filter)
    {
        var stack = new Stack<Assembly>();
        var assembyNames = new HashSet<string>();

        stack.Push(assembly);

        while (stack.Count > 0)
        {
            var asm = stack.Pop();
            if (!filter(asm))
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
}
