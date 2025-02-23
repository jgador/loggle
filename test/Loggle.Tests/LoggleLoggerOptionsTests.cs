using System;
using System.Collections.Generic;
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

    [Fact]
    public void CanReadBatchingOptions()
    {
        var dic = new Dictionary<string, string>
        {
            { "Logging:Loggle:Egress:Kafka:Batching:MaxSize", "100" },
            { "Logging:Loggle:Egress:Kafka:Batching:MaxLifetime", "00:01:00"}
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
        Assert.Equal(100, options!.CurrentValue!.Egress.Kafka?.Batching?.MaxSize);
        Assert.Equal(TimeSpan.FromMinutes(1), options!.CurrentValue!.Egress.Kafka?.Batching?.MaxLifetime);
    }

    [Fact]
    public void CanReadOtlpLoggleOptions()
    {
        var dic = new Dictionary<string, string>
        {
            { "Logging:Loggle:BearerToken", "L0gg|3K3y" },
            { "Logging:Loggle:Endpoint", "http://localhost:4318/v1/logs"}
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(dic!)
            .Build();

        var services = new ServiceCollection();
        services.AddOptions();
        services.AddSingleton<IConfiguration>(configuration);

        services.AddLogging(builder =>
        {
            builder.AddLoggleExporter(configuration);
        });

        var sp = services.BuildServiceProvider();

        var options = sp.GetRequiredService<IOptionsMonitor<LoggleOtlpExporterOptions>>();

        Assert.NotNull(options);
        Assert.NotNull(options.CurrentValue);
        Assert.Equal("L0gg|3K3y", options.CurrentValue.BearerToken);
        Assert.Equal("http://localhost:4318/v1/logs", options.CurrentValue.Endpoint);
    }
}
