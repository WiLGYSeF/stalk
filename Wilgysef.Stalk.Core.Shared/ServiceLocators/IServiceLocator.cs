using System;

namespace Wilgysef.Stalk.Core.Shared.ServiceLocators
{
    public interface IServiceLocator
    {
        IServiceLifetimeScope BeginLifetimeScope();

        T GetService<T>() where T : class;

        object GetService(Type type);

        T GetRequiredService<T>() where T : class;

        object GetRequiredService(Type type);
    }
}
