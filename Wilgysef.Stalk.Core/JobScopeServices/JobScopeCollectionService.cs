using System.Collections.Concurrent;
using Wilgysef.Stalk.Core.Shared.Dependencies;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Core.JobScopeServices;

public class JobScopeCollectionService : IJobScopeCollectionService, ISingletonDependency
{
    private readonly ConcurrentDictionary<long, IServiceLifetimeScope> _scopes = new();

    public IServiceLifetimeScope GetJobScope(long jobId, IServiceLocator serviceLocator)
    {
        if (!_scopes.TryGetValue(jobId, out var scope))
        {
            scope = serviceLocator.BeginLifetimeScopeFromRoot();
            _scopes[jobId] = scope;
        }
        return scope;
    }

    public bool RemoveJobScope(long jobId)
    {
        var success = _scopes.Remove(jobId, out var scope);
        if (success)
        {
            scope?.Dispose();
        }
        return success;
    }
}
