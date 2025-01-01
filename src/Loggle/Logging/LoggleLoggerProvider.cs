using System;
using System.Threading;
using Loggle.Egress;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Loggle.Logging
{
    [ProviderAlias("Loggle")]
    public class LoggleLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private IExternalScopeProvider _scopeProvider = NullExternalScopeProvider.Instance;
        private readonly BufferedChannel<LogMessageEntry> _channel;
        private readonly IEgressLoggerProcessor _egressProcessor;
        private readonly LoggleLoggerOptions _options;
        private readonly Thread _outputThread;

        public LoggleLoggerProvider(
            IOptionsMonitor<LoggleLoggerOptions> options,
            IEgressLoggerProcessor egressProcessor
        )
        {
            ThrowHelper.ThrowIfNull(options?.CurrentValue, nameof(options));

            _options = options.CurrentValue!;
            _egressProcessor = egressProcessor;

            _channel = new BufferedChannel<LogMessageEntry>(
                new BufferedChannelOptions
                {
                    // TODO: Do not be dependent to kafka config
                    MaxSize = _options?.Egress?.Kafka?.Batching?.MaxSize ?? 1_000,
                    MaxLifetime = _options?.Egress?.Kafka?.Batching?.MaxLifetime ?? TimeSpan.FromSeconds(5)
                }, _egressProcessor.EgressAsync);

            _outputThread = new Thread(() =>
            {
                _channel.ConsumeAsync().GetAwaiter().GetResult();
            })
            {
                IsBackground = true,
                Name = "Egress thread"
            };

            _outputThread.Start();
        }

        public ILogger CreateLogger(string name)
            => new LoggleLogger(
                _scopeProvider,
                _channel,
                _options
            );

        public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopeProvider = scopeProvider;

        public void Dispose()
        {
            try
            {
                _outputThread.Join(1500);
            }
            catch (ThreadStateException) { }
        }
    }
}
