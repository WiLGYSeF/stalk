using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Wilgysef.Stalk.Core.Shared.Interfaces;

namespace Wilgysef.Stalk.Core.Shared
{
    public static class ServiceRegistration
    {
        public static IEnumerable<(Type Implementation, Type Service)> GetTransientServiceImplementations(Assembly assembly)
        {
            foreach (var asm in GetAssemblies(assembly, IsEligibleAssembly))
            {
                foreach (var implementation in GetServiceImplementations(typeof(ITransientDependency), asm))
                {
                    yield return implementation;
                }
            }
        }

        public static IEnumerable<(Type Implementation, Type Service)> GetScopedServiceImplementations(Assembly assembly)
        {
            foreach (var asm in GetAssemblies(assembly, IsEligibleAssembly))
            {
                foreach (var implementation in GetServiceImplementations(typeof(IScopedDependency), asm))
                {
                    yield return implementation;
                }
            }
        }

        public static IEnumerable<(Type Implementation, Type Service)> GetSingletonServiceImplementations(Assembly assembly)
        {
            foreach (var asm in GetAssemblies(assembly, IsEligibleAssembly))
            {
                foreach (var implementation in GetServiceImplementations(typeof(ISingletonDependency), asm))
                {
                    yield return implementation;
                }
            }
        }

        private static IEnumerable<(Type Implementation, Type Service)> GetServiceImplementations(Type type, Assembly assembly)
        {
            return assembly.GetTypes()
                .Where(t => t.IsClass
                    && t.GetInterfaces()
                        .Any(i => i.FullName == type.FullName))
                .Select(t => (t, t.GetInterfaces().SingleOrDefault(i => InterfaceSelector(t, i))));
        }

        private static bool InterfaceSelector(Type implementation, Type @interface) => implementation.Name == @interface.Name
            || (@interface.Name.StartsWith("I") && implementation.Name == @interface.Name.Substring(1));

        private static bool IsEligibleAssembly(Assembly assembly) => assembly.FullName != null && assembly.FullName.StartsWith("Wilgysef");

        private static IEnumerable<Assembly> GetAssemblies(Assembly assembly)
        {
            return GetAssemblies(assembly, _ => true);
        }

        private static IEnumerable<Assembly> GetAssemblies(Assembly assembly, Func<Assembly, bool> filter)
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
}
