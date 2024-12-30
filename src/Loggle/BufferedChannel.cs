using System.Threading.Channels;

namespace Loggle;
public class BufferedChannel<TEvent>
{
    private static readonly int MaxCapacity = 15;
    private static readonly TimeSpan MaxAge = TimeSpan.FromSeconds(5);
    private readonly Channel<TEvent> _channel;

    public ChannelWriter<TEvent> Writer => _channel.Writer;

    public BufferedChannel()
    {
        _channel = Channel.CreateBounded<TEvent>(
            new BoundedChannelOptions(MaxCapacity)
            {
                SingleWriter = false,
                SingleReader = false,
                FullMode = BoundedChannelFullMode.Wait
            });
    }

    public async Task ConsumeAsync()
    {
        var reader = _channel.Reader;

        var currentBatch = new List<TEvent>(MaxCapacity);

        while (await reader.WaitToReadAsync().ConfigureAwait(false))
        {
            if (reader.TryRead(out var item))
            {
                currentBatch.Add(item);
            }

            if (currentBatch.Count == 0)
            {
                continue;
            }

            if (currentBatch.Count >= MaxCapacity)
            {
                await FlushBatchAsync(currentBatch).ConfigureAwait(false);

                continue;
            }
        }
    }

    private Task FlushBatchAsync(List<TEvent> batch)
    {
        if (batch.Count == 0)
            return Task.CompletedTask;

        Console.WriteLine($"[Consumer] Flushing {batch.Count} items");
        foreach (var item in batch)
        {
            Console.WriteLine($"  - {item}");
        }

        // Clear the batch
        batch.Clear();

        return Task.CompletedTask;
    }
}
