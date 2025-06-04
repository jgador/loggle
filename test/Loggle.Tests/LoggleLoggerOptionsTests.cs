using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Loggle.Tests;

public class LoggleLoggerOptionsTests
{
    [Fact]
    public void CanReadOtlpLoggleOptions()
    {
        var dic = new Dictionary<string, string>
        {
            { "Logging:Loggle:OtelCollector:BearerToken", "REPLACE_WITH_YOUR_OWN_SECRET" },
            { "Logging:Loggle:OtelCollector:LogsReceiverEndpoint", "http://localhost:4318/v1/logs"}
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(dic!)
            .Build();

        var services = new ServiceCollection();
        services.AddOptions();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLoggleExporter();

        var sp = services.BuildServiceProvider();

        var options = sp.GetRequiredService<IOptionsMonitor<LoggleExporterOptions>>();

        Assert.NotNull(options);
        Assert.NotNull(options.CurrentValue);
        Assert.Equal("REPLACE_WITH_YOUR_OWN_SECRET", options.CurrentValue?.OtelCollector?.BearerToken);
        Assert.Equal("http://localhost:4318/v1/logs", options?.CurrentValue?.OtelCollector?.LogsReceiverEndpoint);
    }
}

// Test: Trigger GitHub Actions workflow for LLM PR review