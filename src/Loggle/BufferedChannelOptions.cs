namespace Loggle;

public class BufferedChannelOptions
{
    public int MaxSize { get; set; } = 1_000;

    public TimeSpan MaxLifetime { get; set; } = TimeSpan.FromSeconds(5);
}
