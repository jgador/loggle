using Loggle.Egress;
using Loggle.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Loggle.Tests;

public class EgressOptionsTests
{
    [Fact]
    public void CanReadEgressOptions()
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

        var options = sp.GetRequiredService<IOptionsMonitor<EgressOptions>>();

        Assert.NotNull(options);
        Assert.NotNull(options.CurrentValue);
        Assert.Equal("localhost:9092", options!.CurrentValue!.Kafka?.BootstrapServers);
        Assert.Equal("demo-topic", options!.CurrentValue!.Kafka?.TopicName);
    }
}
