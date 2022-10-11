using Autofac;
using Autofac.Builder;
using Autofac.Features.Scanning;
using AutoMapper.Contrib.Autofac.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using System.IO.Abstractions;
using System.Reflection;
using Wilgysef.Stalk.Application.AssemblyLoaders;
using Wilgysef.Stalk.Application.HttpClientPolicies;
using Wilgysef.Stalk.Application.IdGenerators;
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
    public bool RegisterExtractors { get; set; } = true;

    public bool RegisterDownloaders { get; set; } = true;

    public int IdGeneratorId { get; set; } = 1;

    /// <summary>
    /// Register services.
    /// </summary>
    /// <param name="services">Service collection.</param>
    public void RegisterServices(IServiceCollection services)
    {
        // Polly registration
        services.AddHttpClient(Constants.HttpClientName)
            .AddHttpClientPolicy();
    }

    /// <summary>
    /// Register DB context.
    /// </summary>
    /// <param name="builder">Container builder.</param>
    /// <param name="options">DbContext options.</param>
    public void RegisterDbContext(ContainerBuilder builder, DbContextOptions<StalkDbContext> options)
    {
        builder.Register(c => options);
        builder.RegisterType<StalkDbContext>()
            // WithParameter is broken?
            //.WithParameter("options", DbContextOptions)
            .As<IStalkDbContext>()
            .As<StalkDbContext>()
            .InstancePerLifetimeScope();
    }

    /// <summary>
    /// Register services.
    /// </summary>
    /// <param name="builder">Container builder.</param>
    public void RegisterServices(
        ContainerBuilder builder,
        ILogger logger,
        IEnumerable<string>? externalAssembliesPaths,
        Func<Type, IOptionSection> getOptionSection)
    {
        var internalAssemblies = GetAssemblies(Assembly.GetCallingAssembly(), EligibleAssemblyFilter).ToArray();
        var externalAssemblies = externalAssembliesPaths != null
            ? externalAssembliesPaths.SelectMany(p => AssemblyLoader.LoadAssemblies(p)).ToArray()
            : Array.Empty<Assembly>();

        var loadedAssemblies = ToArray(
            internalAssemblies.Length + externalAssemblies.Length,
            internalAssemblies.Concat(externalAssemblies));

        // HttpClient registration
        builder.Register(c => c.Resolve<IHttpClientFactory>().CreateClient(Constants.HttpClientName))
            .As<HttpClient>();

        // AutoMapper registration
        builder.RegisterAutoMapper(true, internalAssemblies);

        // IdGen registration
        builder.Register(_ => new IdGenerator(new IdGen.IdGenerator(IdGeneratorId, IdGen.IdGeneratorOptions.Default)))
            .As<IIdGenerator<long>>()
            .SingleInstance();

        // Quartz registration
        builder.Register(_ => new StdSchedulerFactory())
            .As<ISchedulerFactory>()
            .SingleInstance();
        RegisterAssemblyTypes<IJob>(builder, internalAssemblies)
            .AsSelf()
            .InstancePerDependency();

        // System.IO.Abstractions registration
        builder.Register(_ => new FileSystem())
            .As<IFileSystem>()
            .SingleInstance();

        if (logger != null)
        {
            builder.Register(c => logger)
                .As<ILogger>()
                .SingleInstance();
        }

        RegisterAssemblyTypes<ITransientDependency>(builder, internalAssemblies)
            .InstancePerDependency();
        RegisterAssemblyTypes<IScopedDependency>(builder, internalAssemblies)
            .InstancePerLifetimeScope();
        RegisterAssemblyTypes<ISingletonDependency>(builder, internalAssemblies)
            .SingleInstance();

        RegisterAssemblyTypes(typeof(ICommandHandler<,>), builder, internalAssemblies)
            .InstancePerDependency();
        RegisterAssemblyTypes(typeof(IQueryHandler<,>), builder, internalAssemblies)
            .InstancePerDependency();

        var options = internalAssemblies.SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && t.GetInterfaces().Contains(typeof(IOptionSection)));
        foreach (var option in options)
        {
            builder.Register(c => (object)getOptionSection(option))
                .As(option)
                .InstancePerDependency();
        }

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

    private static T[] ToArray<T>(ICollection<T> items)
    {
        return ToArray(items.Count, items);
    }

    private static T[] ToArray<T>(int count, IEnumerable<T> items)
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
