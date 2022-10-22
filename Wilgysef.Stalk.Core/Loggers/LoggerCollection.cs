using Microsoft.Extensions.Logging;
using Wilgysef.Stalk.Core.Shared.Dependencies;

namespace Wilgysef.Stalk.Core.Loggers;

public class LoggerCollectionService : ILoggerCollectionService, ISingletonDependency
{
    private readonly Dictionary<string, LoggerInstance> _loggers = new();

    public LoggerHandle GetLoggerHandle(string path, Func<ILogger> factory)
    {
        lock (_loggers)
        {
            if (!_loggers.TryGetValue(path, out var instance))
            {
                instance = new LoggerInstance(this, path, factory());
            }
            return instance.GetHandle();
        }
    }

    protected bool RemoveLogger(string path)
    {
        lock (_loggers)
        {
            return _loggers.Remove(path);
        }
    }

    private class LoggerInstance
    {
        public ILogger Logger { get; }

        public int Handles { get; set; }

        private object _lock = new();

        private readonly LoggerCollectionService _loggerCollectionService;
        private readonly string _path;

        public LoggerInstance(
            LoggerCollectionService loggerCollectionService,
            string path,
            ILogger logger)
        {
            _loggerCollectionService = loggerCollectionService;
            _path = path;
            Logger = logger;
        }

        public LoggerHandle GetHandle()
        {
            var handle = new LoggerHandle(Logger);

            Handles++;
            handle.Disposing += (_, _) => OnHandleDisposed();

            return handle;
        }

        private void OnHandleDisposed()
        {
            lock (_lock)
            {
                if (--Handles == 0)
                {
                    _loggerCollectionService.RemoveLogger(_path);
                }
            }
        }
    }
}
