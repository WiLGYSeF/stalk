using Microsoft.Extensions.Logging;
using Wilgysef.Stalk.Core.ObjectInstances;

namespace Wilgysef.Stalk.Core.Loggers;

public interface ILoggerCollectionService
{
    IObjectInstanceHandle<ILogger> GetLoggerHandle(string path, Func<ILogger> factory);
}
