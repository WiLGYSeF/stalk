using Autofac;
using Autofac.Builder;
using Autofac.Features.Scanning;
using AutoMapper.Contrib.Autofac.DependencyInjection;
using IdGen;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Wilgysef.Stalk.Core;
using Wilgysef.Stalk.Core.Shared.Cqrs;
using Wilgysef.Stalk.Core.Shared.Dependencies;
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

    public int IdGeneratorId { get; set; } = 1;

    public DbContextOptions<StalkDbContext> DbContextOptions { get; set; }

    public ServiceRegistrar(DbContextOptions<StalkDbContext> options)
    {
        DbContextOptions = options;
    }

    /// <summary>
    /// Register application dependencies.
    /// </summary>
    /// <param name="builder">Container builder.</param>
    public void RegisterApplication(ContainerBuilder builder, IServiceCollection? services = null)
    {
        var assemblies = GetAssemblies(Assembly.GetExecutingAssembly(), EligibleAssemblyFilter)
            .ToArray();

        builder.RegisterAutoMapper(true, assemblies);

        builder.Register(c => new IdGenerator(IdGeneratorId, IdGeneratorOptions.Default))
            .As<IIdGenerator<long>>()
            .SingleInstance();

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
