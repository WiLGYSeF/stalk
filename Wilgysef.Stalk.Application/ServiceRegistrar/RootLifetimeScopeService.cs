using Autofac;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Application.ServiceRegistrar;

public class RootLifetimeScopeService : IRootLifetimeScopeService
{
    public ILifetimeScope LifetimeScope { get; set; } = null!;

    public IServiceLifetimeScope BeginLifetimeScope()
    {
        return new ServiceLifetimeScope(LifetimeScope.BeginLifetimeScope());
    }
}
