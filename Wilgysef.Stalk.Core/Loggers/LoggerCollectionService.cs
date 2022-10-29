using Microsoft.Extensions.Logging;
using Wilgysef.Stalk.Core.ObjectInstances;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.Loggers;

public class LoggerCollectionService : ILoggerCollectionService, ISingletonDependency
{
    private readonly ObjectInstanceCollection<string, ILogger> _loggers = new();

    public IObjectInstanceHandle<ILogger> GetLoggerHandle(string path, Func<ILogger> factory)
    {
        return _loggers.GetHandle(path, factory);
    }
}
