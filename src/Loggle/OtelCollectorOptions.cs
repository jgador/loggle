namespace Loggle
{
    public class OtelCollectorOptions
    {
        public string? LogsReceiverEndpoint { get; set; } = string.Empty;

        public string? BearerToken { get; set; } = string.Empty;
    }
}
