using Autofac;
using Wilgysef.Stalk.Core.Shared.Dependencies;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Application.ServiceRegistrar;

public class ServiceLocator : IServiceLocator, ITransientDependency
{
    private readonly ILifetimeScope _lifetimeScope;

    public ServiceLocator(ILifetimeScope lifetimeScope)
    {
        _lifetimeScope = lifetimeScope;
    }

    public IServiceLifetimeScope BeginLifetimeScope()
    {
        return new ServiceLifetimeScope(_lifetimeScope.BeginLifetimeScope());
    }

    public T GetRequiredService<T>() where T : class
    {
        return _lifetimeScope.Resolve<T>();
    }

    public object GetRequiredService(Type type)
    {
        return _lifetimeScope.Resolve(type);
    }

    public T? GetService<T>() where T : class
    {
        return _lifetimeScope.ResolveOptional<T>();
    }

    public object? GetService(Type type)
    {
        return _lifetimeScope.ResolveOptional(type);
    }
}
