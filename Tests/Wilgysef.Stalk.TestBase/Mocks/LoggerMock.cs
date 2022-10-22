using Microsoft.Extensions.Logging;

namespace Wilgysef.Stalk.TestBase.Mocks;

public class LoggerMock : ILogger
{
    public string Path { get; }

    public LogLevel LogLevel { get; }

    public IReadOnlyList<(LogLevel logLevel, EventId eventId, object? state, Exception? exception)> Logs => _logs;

    private readonly List<(LogLevel logLevel, EventId eventId, object? state, Exception? exception)> _logs = new();

    public LoggerMock(string path, LogLevel logLevel)
    {
        Path = path;
        LogLevel = logLevel;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return new NullDisposable();
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= LogLevel;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _logs.Add((logLevel, eventId, state, exception));
    }

    private class NullDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
