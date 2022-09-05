using Polly;

namespace Wilgysef.Stalk.WebApi.Middleware;

public class LoggingHandler
{
    private ILogger _logger;

    public LoggingHandler(ILogger logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(HttpContext context, Func<Task> next)
    {
        _logger.LogInformation("{METHOD} {PATH}", context.Request.Method, context.Request.Path);
        await next();
    }
}
