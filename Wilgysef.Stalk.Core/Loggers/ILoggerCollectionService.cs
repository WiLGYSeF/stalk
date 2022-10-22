using Microsoft.Extensions.Logging;

namespace Wilgysef.Stalk.Core.Loggers;

public interface ILoggerCollectionService
{
    LoggerHandle GetLoggerHandle(string path, Func<ILogger> factory);
}
