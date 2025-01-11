using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Loggle.Logging;
using Microsoft.Extensions.Options;

namespace Loggle.Egress
{
    public class KafkaEgressLoggerProcessor : IEgressLoggerProcessor
    {
        private readonly KafkaOptions _options;

        public KafkaEgressLoggerProcessor(IOptionsMonitor<LoggleLoggerOptions> loggerOptions
        )
        {
            var kafkaOptions = loggerOptions?.CurrentValue?.Egress?.Kafka;

            if (kafkaOptions is null)
            {
                ThrowHelper.ThrowIfNull(loggerOptions);
            }

            _options = kafkaOptions!;
        }

        public async Task EgressAsync(IReadOnlyList<LogMessageEntry> batch, CancellationToken cancellationToken)
        {
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = _options.BootstrapServers,
                SecurityProtocol = SecurityProtocol.Plaintext,
                SaslMechanism = SaslMechanism.Plain
            };

            using var producer = new ProducerBuilder<Guid, LogMessageEntry>(producerConfig)
                .SetKeySerializer(new SystemTextJsonSerializer<Guid>())
                .SetValueSerializer(new SystemTextJsonSerializer<LogMessageEntry>())
                .Build();

            var producerTasks = batch.Select(async log =>
            {
                await producer.ProduceAsync(
                    _options.TopicName,
                    new Message<Guid, LogMessageEntry>
                    {
                        Key = Guid.NewGuid(),
                        Value = log
                    });
            });


#if NET9_0_OR_GREATER
            await foreach (var handlerTask in Task.WhenEach(producerTasks).ConfigureAwait(false)) { };
#else
            await Task.WhenAll(producerTasks).ConfigureAwait(false);
#endif
        }
    }
}
