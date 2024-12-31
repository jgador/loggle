using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Loggle.Hosting.TestApp;

public class LoggingBackgroundService : BackgroundService
{
    private readonly ILogger<LoggingBackgroundService> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public LoggingBackgroundService(ILoggerFactory loggerFactory, ILogger<LoggingBackgroundService> logger)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int counter = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            counter++;
            _logger.LogInformation("Background task log message # {Counter} - {Guid}", counter, Guid.NewGuid().ToString());
            await Task.Delay(Random.Shared.Next(1, 50), stoppingToken);
        }
    }
}
