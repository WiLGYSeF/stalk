namespace Wilgysef.Stalk.Core.Shared.ServiceLocators
{
    public interface IServiceLocator : IServiceLocatorBase
    {
        /// <summary>
        /// Begins a lifetime scope for services from the root application scope.
        /// </summary>
        /// <returns>Lifetime scope.</returns>
        IServiceLifetimeScope BeginLifetimeScopeFromRoot();
    }
}
