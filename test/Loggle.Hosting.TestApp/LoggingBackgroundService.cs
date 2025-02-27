using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
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
        while (!stoppingToken.IsCancellationRequested)
        {
            var fakePeople = new Faker<FakeUserInfo>()
                .RuleFor(p => p.Name, f => f.Person.FirstName)
                .RuleFor(p => p.Address, f => f.Person.Address.Street)
                .RuleFor(p => p.Email, f => f.Person.Email);

            foreach (var fakePerson in fakePeople.Generate(20))
            {
                _logger.LogInformation(fakePerson.ToString());
            }

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
        while (!stoppingToken.IsCancellationRequested)
        {
            var fakeProducts = new Faker<FakeProduct>()
                .RuleFor(p => p.Description, f => f.Commerce.ProductDescription());

            foreach (var fakeProduct in fakeProducts.Generate(50))
            {
                var levels = Enum.GetValues(typeof(LogLevel))
                         .Cast<LogLevel>()
                         .Where(l => l != LogLevel.None)
                         .ToArray();
                var logLevel = levels[new Random().Next(levels.Length)];

                _logger.Log(logLevel, fakeProduct.Description);
            }

            await Task.Delay(Random.Shared.Next(500, 900), stoppingToken);
        }
    }
}

internal class FakeUserInfo
{
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }

    public override string ToString() => string.Concat(Name, " ", Email, " ", Address);
}

internal class FakeProduct
{
    public string? Description { get; set; }
}
