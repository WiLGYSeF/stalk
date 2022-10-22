using Microsoft.Extensions.Logging;

namespace Wilgysef.Stalk.Core.Loggers;

public interface IAggregateLogger : ILogger
{
    /// <summary>
    /// Loggers.
    /// </summary>
    IEnumerable<ILogger> Loggers { get; }

    /// <summary>
    /// Adds logger.
    /// </summary>
    /// <param name="logger">Logger.</param>
    void AddLogger(ILogger logger);

    /// <summary>
    /// Removes logger.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <returns><see langword="true"/> if the logger was removed, otherwise <see langword="false"/>.</returns>
    bool RemoveLogger(ILogger logger);
}
