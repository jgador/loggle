using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Loggle.Logging
{
    [ProviderAlias("Loggle")]
    public class LoggleLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private IExternalScopeProvider _scopeProvider = NullExternalScopeProvider.Instance;
        private readonly BufferedChannel<LogMessageEntry> _channel;
        private readonly IOptionsMonitor<LoggleLoggerOptions> _options;
        private readonly Thread _outputThread;

        public LoggleLoggerProvider(IOptionsMonitor<LoggleLoggerOptions> options)
        {
            _options = options;

            _channel = new BufferedChannel<LogMessageEntry>(
                new BufferedChannelOptions
                {
                    MaxSize = _options?.CurrentValue?.Egress?.Kafka?.Batching?.MaxSize ?? 1_000,
                    MaxLifetime = _options?.CurrentValue?.Egress?.Kafka?.Batching?.MaxLifetime ?? TimeSpan.FromSeconds(5)
                }, SendToKafkaAsync);

            _outputThread = new Thread(Flush)
            {
                IsBackground = true,
                Name = "Loggle logger queue processing thread"
            };

            _outputThread.Start();
        }

        private void Flush()
        {
            _channel.ConsumeAsync().GetAwaiter().GetResult();
        }

        public ILogger CreateLogger(string name) => new LoggleLogger(name, _scopeProvider, _channel, _options.CurrentValue);

        public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopeProvider = scopeProvider;

        private static ValueTask FlushBatchHandlerAsync(IReadOnlyList<LogMessageEntry> batch)
        {
            if (batch.Count == 0)
                return ValueTask.CompletedTask;

            Debug.WriteLine($"[Consumer] Flushing {batch.Count} item(s): {string.Join(",", batch.Select(l => l.Message))}");

            return ValueTask.CompletedTask;
        }

        private async ValueTask SendToKafkaAsync(IReadOnlyList<LogMessageEntry> batch)
        {
            try
            {
                var bootstrapServers = _options.CurrentValue.Egress.Kafka.BootstrapServers;
                var topicName = _options.CurrentValue.Egress.Kafka.TopicName;

                var producerConfig = new ProducerConfig
                {
                    BootstrapServers = bootstrapServers,
                    SecurityProtocol = SecurityProtocol.Plaintext,
                    SaslMechanism = SaslMechanism.Plain
                };

                using var producer = new ProducerBuilder<string, string>(producerConfig)
                   .Build();

                foreach (var item in batch)
                {
                    var result = await producer.ProduceAsync(topicName, new Message<string, string>
                    {
                        Key = Guid.NewGuid().ToString(),
                        Value = item.Message
                    });

                    Console.WriteLine($"Message produced to {result.TopicPartitionOffset}");
                }
            }
            catch (Exception ex)
            {
            }
        }

        public void Dispose()
        {
        }
    }
}
