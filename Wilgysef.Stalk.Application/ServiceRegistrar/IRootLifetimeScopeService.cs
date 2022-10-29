using Autofac;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Application.ServiceRegistrar;

public interface IRootLifetimeScopeService
{
    ILifetimeScope LifetimeScope { get; set; }

    IServiceLifetimeScope BeginLifetimeScope();
}
