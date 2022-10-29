using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Wilgysef.Stalk.Core.Shared.Dependencies;
using Wilgysef.Stalk.Core.Shared.ServiceLocators;

namespace Wilgysef.Stalk.Core.JobScopeServices;

public class JobScopeCollectionService : IJobScopeCollectionService, ISingletonDependency
{
    private readonly ConcurrentDictionary<long, IServiceLifetimeScope> _scopes = new();
    private readonly ConcurrentDictionary<long, ILogger?> _loggers = new();

    public IServiceLifetimeScope GetJobScope(long jobId, IServiceLocator serviceLocator)
    {
        if (!_scopes.TryGetValue(jobId, out var scope))
        {
            scope = serviceLocator.BeginLifetimeScopeFromRoot();
            _scopes[jobId] = scope;
        }
        return scope;
    }

    public void AddLoggerToScope(long jobId, ILogger? logger)
    {
        _loggers.TryAdd(jobId, logger);
    }

    public ILogger? GetLoggerFromScope(long jobId)
    {
        return _loggers[jobId];
    }

    public bool RemoveJobScope(long jobId)
    {
        var success = _scopes.TryRemove(jobId, out var scope);
        if (success)
        {
            _loggers.TryRemove(jobId, out _);
            scope?.Dispose();
        }
        return success;
    }
}
