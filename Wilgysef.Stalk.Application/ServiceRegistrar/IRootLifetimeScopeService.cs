using Autofac;
using Wilgysef.Stalk.Core.Shared.Dependencies;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Application.ServiceRegistrar;

public interface IRootLifetimeScopeService : ISingletonDependency
{
    ILifetimeScope LifetimeScope { get; set; }

    IServiceLifetimeScope BeginLifetimeScope();
}
