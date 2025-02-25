using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter.Loggle;

namespace Loggle.Tests;

public class LoggleLoggerOptionsTests
{
    [Fact]
    public void CanReadOtlpLoggleOptions()
    {
        var dic = new Dictionary<string, string>
        {
            { "Logging:Loggle:OtelCollector:BearerToken", "L0gg|3K3y" },
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
        Assert.Equal("L0gg|3K3y", options.CurrentValue?.OtelCollector?.BearerToken);
        Assert.Equal("http://localhost:4318/v1/logs", options?.CurrentValue?.OtelCollector?.LogsReceiverEndpoint);
    }
}
