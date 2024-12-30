using Loggle.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Loggle.Tests;

public class LoggleLoggerOptionsTests
{
    [Fact]
    public void CanReadLoggleLoggerOptions()
    {
        var dic = new Dictionary<string, string>
        {
            { "Logging:Loggle:Egress:Kafka:BootstrapServers", "localhost:9092" },
            { "Logging:Loggle:Egress:Kafka:TopicName", "demo-topic"}
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(dic!)
            .Build();

        var services = new ServiceCollection();
        services.AddOptions();
        services.AddSingleton<IConfiguration>(configuration);

        services.AddLogging(builder =>
        {
            builder.AddLoggle();
        });

        var sp = services.BuildServiceProvider();

        var options = sp.GetRequiredService<IOptionsMonitor<LoggleLoggerOptions>>();

        Assert.NotNull(options);
        Assert.NotNull(options.CurrentValue);
        Assert.Equal("localhost:9092", options!.CurrentValue!.Egress.Kafka?.BootstrapServers);
        Assert.Equal("demo-topic", options!.CurrentValue!.Egress.Kafka?.TopicName);
    }

    [Fact]
    public void CanReadDefaultLogLevelOfLoggleLogger()
    {
        var dic = new Dictionary<string, string>
        {
            { "Logging:Loggle:MinimumLevel", "Debug" },
            { "Logging:Loggle:Egress:Kafka:BootstrapServers", "localhost:9092" },
            { "Logging:Loggle:Egress:Kafka:TopicName", "demo-topic" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(dic)
            .Build();

        var services = new ServiceCollection();
        services.AddOptions();
        services.AddSingleton<IConfiguration>(configuration);

        services.AddLogging(builder =>
        {
            builder.AddLoggle();
        });

        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptionsMonitor<LoggleLoggerOptions>>();

        Assert.NotNull(options);
        Assert.NotNull(options.CurrentValue);
        Assert.Equal(LogLevel.Debug, options.CurrentValue.MinimumLevel);
    }
}
