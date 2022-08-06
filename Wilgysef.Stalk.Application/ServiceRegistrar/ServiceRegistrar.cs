using Autofac;
using AutoMapper.Contrib.Autofac.DependencyInjection;
using IdGen;
using System.Reflection;
using Wilgysef.Stalk.Core;
using Wilgysef.Stalk.Core.Shared.Cqrs;
using Wilgysef.Stalk.Core.Shared.Dependencies;
using Wilgysef.Stalk.EntityFrameworkCore;

namespace Wilgysef.Stalk.Application.ServiceRegistrar;

public class ServiceRegistrar
{
    private static readonly Type[] DependsOn = new[]
    {
        typeof(ApplicationModule),
        typeof(CoreModule),
        typeof(EntityFrameworkCoreModule),
    };

    public int IdGeneratorId { get; set; } = 1;

    /// <summary>
    /// Register application dependencies.
    /// </summary>
    /// <param name="builder"></param>
    public void RegisterApplication(ContainerBuilder builder)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblies = GetAssemblies(assembly, EligibleAssemblyFilter).ToArray();

        builder.RegisterAutoMapper(true, assemblies);

        builder.Register(c => new IdGenerator(IdGeneratorId, IdGeneratorOptions.Default))
            .As<IIdGenerator<long>>()
            .SingleInstance();

        builder.RegisterAssemblyTypes(assemblies)
            .Where(t => t.GetInterfaces().Any(i => i.IsAssignableFrom(typeof(ITransientDependency))))
            .AsImplementedInterfaces()
            .PropertiesAutowired()
            .InstancePerDependency();

        builder.RegisterAssemblyTypes(assemblies)
            .Where(t => t.GetInterfaces().Any(i => i.IsAssignableFrom(typeof(IScopedDependency))))
            .AsImplementedInterfaces()
            .PropertiesAutowired()
            .InstancePerLifetimeScope();

        builder.RegisterAssemblyTypes(assemblies)
            .Where(t => t.GetInterfaces().Any(i => i.IsAssignableFrom(typeof(ISingletonDependency))))
            .AsImplementedInterfaces()
            .PropertiesAutowired()
            .SingleInstance();

        builder.RegisterAssemblyTypes(assemblies)
            .Where(t => t.GetInterfaces().Any(i => i.IsAssignableFrom(typeof(ICommandHandler<,>))))
            .AsImplementedInterfaces()
            .PropertiesAutowired()
            .InstancePerDependency();

        builder.RegisterAssemblyTypes(assemblies)
            .Where(t => t.GetInterfaces().Any(i => i.IsAssignableFrom(typeof(IQueryHandler<,>))))
            .AsImplementedInterfaces()
            .PropertiesAutowired()
            .InstancePerDependency();
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
