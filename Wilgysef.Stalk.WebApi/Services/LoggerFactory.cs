using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Wilgysef.Stalk.Core.Shared.Dependencies;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using ILoggerFactory = Wilgysef.Stalk.Core.Shared.Loggers.ILoggerFactory;

namespace Wilgysef.Stalk.WebApi.Services;

public class LoggerFactory : ILoggerFactory, ISingletonDependency
{
    public ILogger CreateLogger(string path, LogLevel logLevel)
    {
        var loggerFactory = new SerilogLoggerFactory(
            new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(path, restrictedToMinimumLevel: GetLogEventLevel(logLevel))
                .CreateLogger());
        return loggerFactory.CreateLogger("default");
    }

    private static LogEventLevel GetLogEventLevel(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => LogEventLevel.Verbose,
            LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Information => LogEventLevel.Information,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Error => LogEventLevel.Error,
            LogLevel.Critical => LogEventLevel.Fatal,
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel)),
        };
    }
}
