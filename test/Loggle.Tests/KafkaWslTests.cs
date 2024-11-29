using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace Loggle.Tests;
public class KafkaWslTests
{
    private const string BootstrapServer = "localhost:9092";

    [Fact]
    public async Task CreateTopic()
    {
        var topicName = "wsl-demo-topic";

        var config = new AdminClientConfig
        {
            BootstrapServers = BootstrapServer
        };

        using var client = new AdminClientBuilder(config)
            .Build();

        await client.CreateTopicsAsync(new[]
        {
            new TopicSpecification
            {
                Name = topicName,
                NumPartitions = 3,
                ReplicationFactor = 1
            }
        });
    }
}
