namespace Wilgysef.Stalk.Application.ServiceRegistrar;

public class ServiceRegistration
{
    public Type Implementation { get; }

    public Type Service { get; }

    public ServiceRegistration(Type implementation, Type service)
    {
        Implementation = implementation;
        Service = service;
    }
}
