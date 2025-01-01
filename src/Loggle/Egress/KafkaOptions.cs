namespace Loggle.Egress
{
    public class KafkaOptions
    {
        public string? BootstrapServers { get; set; }

        public string? TopicName { get; set; }

        public BufferedChannelOptions? Batching { get; set; }
    }
}
