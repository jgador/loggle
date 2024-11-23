using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace Loggle.Tests;

public sealed class ConfluentKafkaFixture : IAsyncLifetime, IDisposable
{
    private const string KafkaBootstrapServers = "localhost:9092";

    public AdminClientConfig AdminClientConfig { get; }
    public ProducerConfig ProducerConfig { get; }
    public ConsumerConfig ConsumerConfig { get; }

    public ConfluentKafkaFixture()
    {
        AdminClientConfig = new AdminClientConfig { BootstrapServers = KafkaBootstrapServers };
        ProducerConfig = new ProducerConfig { BootstrapServers = KafkaBootstrapServers };
        ConsumerConfig = new ConsumerConfig
        {
            BootstrapServers = KafkaBootstrapServers,
            GroupId = "test-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };
    }

    public async Task CreateTopicAsync(string topicName, int numPartitions = 1, short replicationFactor = 1)
    {
        using var adminClient = new AdminClientBuilder(AdminClientConfig).Build();
        try
        {
            await adminClient.CreateTopicsAsync(new[]
            {
                    new TopicSpecification
                    {
                        Name = topicName,
                        NumPartitions = numPartitions,
                        ReplicationFactor = replicationFactor
                    }
                });

            Console.WriteLine($"Topic '{topicName}' created successfully.");
        }
        catch (CreateTopicsException ex)
        {
            if (ex.Results.Any(r => r.Error.Code != ErrorCode.TopicAlreadyExists))
            {
                throw;
            }

            Console.WriteLine($"Topic '{topicName}' already exists.");
        }
    }

    public void Dispose()
    {
    }

    public Task DisposeAsync() => Task.CompletedTask;
    public Task InitializeAsync() => Task.CompletedTask;
}
