namespace Loggle.Egress;

public class EgressOptions
{
    public string Type { get; set; } = EgressProviderTypes.Kafka;

    public KafkaOptions? Kafka { get; set; }
}
