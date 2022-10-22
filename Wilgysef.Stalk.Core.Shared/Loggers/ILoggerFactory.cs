using Microsoft.Extensions.Logging;

namespace Wilgysef.Stalk.Core.Shared.Loggers
{
    public interface ILoggerFactory
    {
        ILogger CreateLogger(string path, LogLevel logLevel);
    }
}
