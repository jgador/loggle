namespace Loggle.Egress;

public class EgressOptions
{
    public const string SectionKey = "Logging:Loggle:Egress";

    public KafkaOptions? Kafka { get; set; }
}
