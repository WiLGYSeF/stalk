using Microsoft.Extensions.Logging;

namespace Wilgysef.Stalk.Core.Loggers;

public class AggregateLogger : IAggregateLogger
{
    public IEnumerable<ILogger> Loggers => _loggers;

    private readonly List<ILogger> _loggers = new();

    public AggregateLogger(IEnumerable<ILogger?> loggers)
    {
        foreach (var logger in loggers)
        {
            if (logger != null)
            {
                _loggers.Add(logger);
            }
        }
    }

    public AggregateLogger(params ILogger?[] loggers) : this((IEnumerable<ILogger?>)loggers) { }

    public void AddLogger(ILogger logger)
    {
        _loggers.Add(logger);
    }

    public bool RemoveLogger(ILogger logger)
    {
        return _loggers.Remove(logger);
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return new AggregateDisposable<IDisposable>(_loggers.Select(l => l.BeginScope(state)));
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _loggers.Any(l => l.IsEnabled(logLevel));
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        foreach (var logger in _loggers)
        {
            logger.Log(logLevel, eventId, state, exception, formatter);
        }
    }

    private class AggregateDisposable<T> : IDisposable where T : IDisposable
    {
        private readonly List<T> _disposables;

        public AggregateDisposable(IEnumerable<T> disposables)
        {
            _disposables = new(disposables);
        }

        public void Dispose()
        {
            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }
        }
    }
}
