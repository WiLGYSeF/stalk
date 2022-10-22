using Microsoft.Extensions.Logging;
using ILoggerFactory = Wilgysef.Stalk.Core.Shared.Loggers.ILoggerFactory;

namespace Wilgysef.Stalk.TestBase.Mocks;

public class LoggerFactoryMock : ILoggerFactory
{
    public IEnumerable<LoggerMock> Loggers => _loggers;

    private readonly List<LoggerMock> _loggers = new();

    public ILogger CreateLogger(string path, LogLevel logLevel)
    {
        var logger = new LoggerMock(path, logLevel);
        _loggers.Add(logger);
        return logger;
    }
}
