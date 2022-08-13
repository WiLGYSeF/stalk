using System;

namespace Wilgysef.Stalk.Core.Shared.ServiceLocators
{
    public interface IServiceLifetimeScope : IDisposable
    {
        /// <summary>
        /// Begins a lifetime scope for services.
        /// </summary>
        /// <returns>Lifetime scope.</returns>
        IServiceLifetimeScope BeginLifetimeScope();

        /// <summary>
        /// Gets service.
        /// </summary>
        /// <typeparam name="T">Service type.</typeparam>
        /// <returns>Service, or <see langword="null"/> if service is not registered.</returns>
        T GetService<T>() where T : class;

        /// <summary>
        /// Gets service.
        /// </summary>
        /// <param name="type">Service type.</param>
        /// <returns>Service, or <see langword="null"/> if service is not registered.</returns>
        object GetService(Type type);

        /// <summary>
        /// Gets service. Throws if service is not registered.
        /// </summary>
        /// <typeparam name="T">Service type.</typeparam>
        /// <returns>Service.</returns>
        T GetRequiredService<T>() where T : class;

        /// <summary>
        /// Gets service. Throws if service is not registered.
        /// </summary>
        /// <param name="type">Service type.</param>
        /// <returns>Service.</returns>
        object GetRequiredService(Type type);
    }
}
