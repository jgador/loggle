using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Loggle.Hosting.TestApp;

public class LoggingBackgroundService : BackgroundService
{
    private readonly ILogger<LoggingBackgroundService> _logger;

    public LoggingBackgroundService(ILogger<LoggingBackgroundService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int counter = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            counter++;
            _logger.LogInformation("LoggingBackgroundService # {Counter} - {Guid}", counter, Guid.NewGuid().ToString());
            await Task.Delay(Random.Shared.Next(500, 900), stoppingToken);
        }
    }
}

public class YetAnotherLoggingBackgroundService : BackgroundService
{
    private readonly ILogger<YetAnotherLoggingBackgroundService> _logger;

    public YetAnotherLoggingBackgroundService(ILogger<YetAnotherLoggingBackgroundService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int counter = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            counter++;
            _logger.LogInformation("YetAnotherLoggingBackgroundService # {Counter} - {Guid}", counter, Guid.NewGuid().ToString());
            await Task.Delay(Random.Shared.Next(500, 900), stoppingToken);
        }
    }
}
