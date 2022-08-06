using System.Reflection;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Application.ServiceRegistrar;

public class ServiceRegistrar
{
    /// <summary>
    /// Get transient service implementations for registration.
    /// </summary>
    /// <param name="assembly">Executing assembly.</param>
    /// <returns>Implementations and their services.</returns>
    public IEnumerable<(Type Implementation, Type Service)> GetTransientServiceImplementations(Assembly assembly)
    {
        return GetAssembliesServiceImplementations<ITransientDependency>(assembly);
    }

    /// <summary>
    /// Get scoped service implementations for registration.
    /// </summary>
    /// <param name="assembly">Executing assembly.</param>
    /// <returns>Implementations and their services.</returns>
    public IEnumerable<(Type Implementation, Type Service)> GetScopedServiceImplementations(Assembly assembly)
    {
        return GetAssembliesServiceImplementations<IScopedDependency>(assembly);
    }

    /// <summary>
    /// Get singleton service implementations for registration.
    /// </summary>
    /// <param name="assembly">Executing assembly.</param>
    /// <returns>Implementations and their services.</returns>
    public IEnumerable<(Type Implementation, Type Service)> GetSingletonServiceImplementations(Assembly assembly)
    {
        return GetAssembliesServiceImplementations<ISingletonDependency>(assembly);
    }

    /// <summary>
    /// Get service implementations for registration of <paramref name="service"/> type.
    /// </summary>
    /// <param name="service">Service.</param>
    /// <param name="assembly">Executing assembly.</param>
    /// <returns>Implementations and their services.</returns>
    public IEnumerable<(Type Implementation, Type Service)> GetServiceTypeImplementations(Type service, Assembly assembly)
    {
        return GetAssembliesServiceImplementations(service, assembly, (_, @interface) =>
        {
            return @interface.FullName == service.FullName;
        });
    }

    /// <summary>
    /// Get service implementations for registration.
    /// </summary>
    /// <typeparam name="T">Service</typeparam>
    /// <param name="assembly">Executing assembly.</param>
    /// <returns>Implementations and their services.</returns>
    public IEnumerable<(Type Implementation, Type Service)> GetServiceTypeImplementations<T>(Assembly assembly)
    {
        return GetServiceTypeImplementations(typeof(T), assembly);
    }

    /// <summary>
    /// Get all referenced assemblies.
    /// </summary>
    /// <param name="assembly">Parent assembly.</param>
    /// <returns>All referenced assemblies of <paramref name="assembly"/>.</returns>
    public IEnumerable<Assembly> GetAssemblies(Assembly assembly)
    {
        return GetAssemblies(assembly, _ => true);
    }

    private IEnumerable<(Type Implementation, Type Service)> GetAssembliesServiceImplementations(
        Type type,
        Assembly assembly,
        Func<Type, Type, bool> interfaceSelector)
    {
        foreach (var asm in GetAssemblies(assembly, IsEligibleAssembly))
        {
            foreach (var implementation in GetServiceImplementations(type, asm, interfaceSelector))
            {
                yield return implementation;
            }
        }
    }

    private IEnumerable<(Type Implementation, Type Service)> GetAssembliesServiceImplementations<T>(
        Assembly assembly,
        Func<Type, Type, bool> interfaceSelector)
    {
        return GetAssembliesServiceImplementations(typeof(T), assembly, interfaceSelector);
    }

    private IEnumerable<(Type Implementation, Type Service)> GetAssembliesServiceImplementations<T>(Assembly assembly)
    {
        return GetAssembliesServiceImplementations<T>(assembly, InterfaceSelector);
    }

    private IEnumerable<(Type Implementation, Type Service)> GetServiceImplementations(
        Type type,
        Assembly assembly,
        Func<Type, Type, bool> interfaceSelector)
    {
        return assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract
                && t.GetInterfaces()
                    .Any(i => i.FullName == type.FullName))
            .Select(t =>
            {
                var interfaces = t.GetInterfaces().ToList();
                return (
                    t,
                    interfaces.SingleOrDefault(i => interfaceSelector(t, i)) ?? interfaces.Single(ii => ii.FullName != type.FullName));
            });
    }

    private IEnumerable<(Type Implementation, Type Service)> GetServiceImplementations(Type type, Assembly assembly)
    {
        return GetServiceImplementations(type, assembly, InterfaceSelector);
    }

    private bool InterfaceSelector(Type implementation, Type @interface) => implementation.Name == @interface.Name
        || (@interface.Name.StartsWith("I") && implementation.Name == @interface.Name.Substring(1));

    private bool IsEligibleAssembly(Assembly assembly) => assembly.FullName != null && assembly.FullName.StartsWith("Wilgysef");

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
