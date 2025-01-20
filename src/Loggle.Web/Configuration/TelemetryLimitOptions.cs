namespace Loggle.Web.Configuration;

public class TelemetryLimitOptions
{
    public int MaxLogCount { get; set; } = 10_000;

    public int MaxAttributeCount { get; set; } = 128;

    public int MaxAttributeLength { get; set; } = int.MaxValue;
}
