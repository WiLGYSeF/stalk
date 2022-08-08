using Autofac;
using Wilgysef.Stalk.Core.Shared.Dependencies;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Application.ServiceRegistrar;

public class ServiceLocator : IServiceLocator, ITransientDependency
{
    private readonly IComponentContext _componentContext;

    public ServiceLocator(IComponentContext componentContext)
    {
        _componentContext = componentContext;
    }

    public T GetRequiredService<T>() where T : class
    {
        return _componentContext.Resolve<T>();
    }

    public object GetRequiredService(Type type)
    {
        return _componentContext.Resolve(type);
    }

    public T? GetService<T>() where T : class
    {
        return _componentContext.ResolveOptional<T>();
    }

    public object? GetService(Type type)
    {
        return _componentContext.ResolveOptional(type);
    }
}
