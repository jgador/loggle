using Confluent.Kafka;

namespace Loggle.Tests;

public class ConfluentKafkaTest : IClassFixture<ConfluentKafkaFixture>
{
    private readonly ConfluentKafkaFixture _fixture;

    public ConfluentKafkaTest(ConfluentKafkaFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CanCreateAndProduceToTopic()
    {
        const string topicName = "loggle-test";

        // Create topic
        // await _fixture.CreateTopicAsync(topicName, 1, 3);

        // Produce a message to the topic
        using var producer = new ProducerBuilder<string, string>(_fixture.ProducerConfig)
            .Build();

        for (int i = 0; i < 10; i++)
        {
            var result = await producer.ProduceAsync(topicName, new Message<string, string>
            {
                Key = $"key{i}",
                Value = $"Hello, Kafka! {i}"
            });

            Assert.NotNull(result);
            Console.WriteLine($"Message produced to {result.TopicPartitionOffset}");
        }
    }

    [Fact]
    public async Task CanConsumeFromTopic()
    {
        const string topicName = "loggle-test";

        // Consume a message from the topic
        using var consumer = new ConsumerBuilder<Ignore, string>(_fixture.ConsumerConfig)
            .Build();

        consumer.Subscribe(topicName);

        var consumeResult = consumer.Consume(TimeSpan.FromSeconds(10));
        //Assert.NotNull(consumeResult);
        //Assert.Equal("Hello, Kafka!", consumeResult.Message.Value);

        Console.WriteLine($"Consumed message: {consumeResult.Message.Value}");
    }
}
