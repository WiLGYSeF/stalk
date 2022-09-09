﻿using Autofac;
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

    /// <summary>
    /// Register services.
    /// </summary>
    /// <param name="services">Service collection.</param>
    public void RegisterServices(IServiceCollection services)
    {
        // Polly registration
        services.AddHttpClient(Constants.HttpClientName)
            .AddExtractorDownloaderClientPolicy();
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
        string? externalAssembliesPath,
        Func<Type, IOptionSection> getOptionSection)
    {
        var assemblies = GetAssemblies(Assembly.GetExecutingAssembly(), EligibleAssemblyFilter).ToList();
        var externalAssemblies = externalAssembliesPath != null
            ? AssemblyLoader.LoadAssemblies(externalAssembliesPath)
            : new List<Assembly>();

        var loadedAssemblies = ToArray(
            assemblies.Count + externalAssemblies.Count,
            assemblies.Concat(externalAssemblies));

        // HttpClient registration
        builder.Register(c => c.Resolve<IHttpClientFactory>().CreateClient(Constants.HttpClientName))
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

        if (logger != null)
        {
            builder.Register(c => logger)
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

        var options = loadedAssemblies.SelectMany(a => a.GetTypes())
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
