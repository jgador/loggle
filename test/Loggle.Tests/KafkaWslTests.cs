using System.Threading.Channels;
using System.Threading.Tasks;
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

    [Fact]
    public void WaitToReadAsync_DataAvailableBefore_CompletesSynchronously()
    {
        Channel<int> c = Channel.CreateBounded<int>(1);
        ValueTask write = c.Writer.WriteAsync(42);
        ValueTask<bool> read = c.Reader.WaitToReadAsync();
        Assert.True(read.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task WaitToReadAsync_DataAvailableBefore_CompletesAsynchronously()
    {
        Channel<int> c = Channel.CreateBounded<int>(1);
        await c.Writer.WriteAsync(42).ConfigureAwait(false);
        bool read = await c.Reader.WaitToReadAsync().ConfigureAwait(false);

        c.Writer.Complete();

        bool read2 = await c.Reader.WaitToReadAsync().ConfigureAwait(false);
        Assert.True(read);
    }
}
