using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Loggle
{
    public delegate ValueTask BufferedChannelFlushHandler<in TEvent>(IReadOnlyList<TEvent> batch);

    public class BufferedChannel<TEvent>
    where TEvent : notnull
    {
        private readonly Channel<TEvent> _channel;
        private readonly BufferedChannelOptions _options;
        private readonly BufferedChannelFlushHandler<TEvent> _flushHandler;

        public ChannelWriter<TEvent> Writer => _channel.Writer;

        public ChannelReader<TEvent> Reader => _channel.Reader;

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
            try
            {
                var maxSize = _options!.MaxSize;

                var currentBatch = new List<TEvent>(maxSize);
                var startTime = DateTimeOffset.UtcNow;

                // Reader.Completion is the Task that completes when no more data
                // will ever be available to read from this channel (for example,
                // when the writer is completed).

                while (
                    await Reader.WaitToReadAsync().ConfigureAwait(false)
                    && Reader.Completion.Status != TaskStatus.RanToCompletion
                )
                {
                    var item = await Reader.ReadAsync().ConfigureAwait(false);

                    if (item is not null)
                    {
                        currentBatch.Add(item);
                    }

                    if (currentBatch.Count >= maxSize || IsPastMaxLifetime(startTime))
                    {
                        await FlushBufferAsync().ConfigureAwait(false);
                    }
                }

                if (currentBatch.Count > 0)
                {
                    await FlushBufferAsync().ConfigureAwait(false);
                }

                async ValueTask FlushBufferAsync()
                {
                    var batch = currentBatch.ToArray();
                    await _flushHandler(batch).ConfigureAwait(false);
                    currentBatch.Clear();
                    startTime = DateTimeOffset.UtcNow;

                    Debug.Assert(batch.Length > 0, "Should not be affected when currentBatch is cleared");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Consumer]: {ex.Message}");
                throw;
            }
        }

        private bool IsPastMaxLifetime(DateTimeOffset startTime) =>
            startTime.Add(_options!.MaxLifetime) < DateTimeOffset.UtcNow;
    }
}
