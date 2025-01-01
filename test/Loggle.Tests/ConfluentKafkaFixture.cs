using System;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace Loggle.Tests;

public sealed class ConfluentKafkaFixture : IAsyncLifetime, IDisposable
{
    private const string KafkaBootstrapServers = "pkc-ldvr1.asia-southeast1.gcp.confluent.cloud:9092";//"localhost:9092";

    public AdminClientConfig AdminClientConfig { get; }
    public ProducerConfig ProducerConfig { get; }
    public ConsumerConfig ConsumerConfig { get; }

    public ConfluentKafkaFixture()
    {
        AdminClientConfig = new AdminClientConfig
        {
            BootstrapServers = KafkaBootstrapServers,
            SecurityProtocol = SecurityProtocol.SaslSsl,
            SaslMechanism = SaslMechanism.Plain,
            ApiVersionRequest = true,
            SaslUsername = "",
            SaslPassword = "",
            ClientId = "ccloud-csharp-client-3f111376-f4c9-4e26-9261-e3d91f10f624"
        };

        ProducerConfig = new ProducerConfig
        {
            BootstrapServers = KafkaBootstrapServers,
            SecurityProtocol = SecurityProtocol.SaslSsl,
            SaslMechanism = SaslMechanism.Plain,
            SaslUsername = "",
            SaslPassword = "",
            ClientId = "ccloud-csharp-client-d3ca6def-bbcc-4924-8d99-ba206d1f6162"
        };

        ConsumerConfig = new ConsumerConfig
        {
            BootstrapServers = KafkaBootstrapServers,
            SecurityProtocol = SecurityProtocol.SaslSsl,
            SaslMechanism = SaslMechanism.Plain,
            SaslUsername = "",
            SaslPassword = "",
            ClientId = "ccloud-csharp-client-bf0c0f2f-7d02-4370-b634-3c69912f1902",
            GroupId = "csharp-group-1",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };
    }

    public async Task CreateTopicAsync(string topicName, int numPartitions = 1, short replicationFactor = 1)
    {
        using var adminClient = new AdminClientBuilder(AdminClientConfig)
            .Build();

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
