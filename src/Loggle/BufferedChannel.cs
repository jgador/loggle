using System.Diagnostics;
using System.Threading.Channels;

namespace Loggle;

public delegate ValueTask BufferedChannelFlushHandler<in TEvent>(IReadOnlyList<TEvent> batch);

public class BufferedChannel<TEvent>
{
    private readonly Channel<TEvent> _channel;
    private readonly BufferedChannelOptions _options;
    private readonly BufferedChannelFlushHandler<TEvent> _flushHandler;

    public ChannelWriter<TEvent> Writer => _channel.Writer;

    public BufferedChannel(
        BufferedChannelOptions options,
        BufferedChannelFlushHandler<TEvent> flushHandler)
    {
        ThrowHelper.ThrowIfNull(nameof(options));
        ThrowHelper.ThrowIfNull(nameof(flushHandler));

        _options = options;

        _channel = Channel.CreateBounded<TEvent>(
            new BoundedChannelOptions(_options!.MaxSize)
            {
                SingleWriter = false,
                SingleReader = false,
                FullMode = BoundedChannelFullMode.Wait
            });

        _flushHandler = flushHandler;
    }

    public async ValueTask ConsumeAsync()
    {
        var maxSize = _options!.MaxSize;
        var reader = _channel.Reader;

        var currentBatch = new List<TEvent>(maxSize);
        var startTime = DateTime.UtcNow;

        var canRead = await reader.WaitToReadAsync().ConfigureAwait(false);

        while (canRead)
        {
            if (reader.TryRead(out var item))
            {
                Debug.WriteLine($"Reading from channel: {item}");
                currentBatch.Add(item);
            }

            if (currentBatch.Count >= maxSize || IsPastMaxLifetime(startTime))
            {
                Debug.WriteLine($"IsPastMaxLifetime: {IsPastMaxLifetime(startTime)} - Start: {startTime.ToString("O")}, Start + Lifetime: {startTime.Add(_options!.MaxLifetime).ToString("O")}");

                var batch = currentBatch.ToArray();
                await _flushHandler(batch).ConfigureAwait(false);
                currentBatch.Clear();
                startTime = DateTime.UtcNow;
            }
            else
            {
                continue;
            }
        }
    }

    private bool IsPastMaxLifetime(DateTime startTime) =>
        startTime.Add(_options!.MaxLifetime) < DateTime.UtcNow;
}
