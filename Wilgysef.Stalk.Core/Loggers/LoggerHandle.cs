using Microsoft.Extensions.Logging;

namespace Wilgysef.Stalk.Core.Loggers;

public class LoggerHandle : IDisposable
{
    public ILogger Logger { get; }

    public event EventHandler? Disposing;

    public LoggerHandle(ILogger logger)
    {
        Logger = logger;
    }

    public void Dispose()
    {
        Disposing?.Invoke(this, EventArgs.Empty);

        GC.SuppressFinalize(this);
    }
}
