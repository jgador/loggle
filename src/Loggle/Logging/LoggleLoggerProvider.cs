using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Confluent.Kafka;
using Loggle.Egress;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Loggle.Logging;

[ProviderAlias("Loggle")]
public class LoggleLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private IExternalScopeProvider _scopeProvider = NullExternalScopeProvider.Instance;
    private Channel<LogMessageEntry> _channel;
    private readonly IOptionsMonitor<LoggleLoggerOptions> _options;
    private readonly IOptionsMonitor<EgressOptions> _egressOptions;
    private readonly Task _backgroundTask;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public LoggleLoggerProvider(IOptionsMonitor<LoggleLoggerOptions> options, IOptionsMonitor<EgressOptions> egressOptions)
    {
        _options = options;
        _egressOptions = egressOptions;
        _channel = Channel.CreateBounded<LogMessageEntry>(new BoundedChannelOptions(2)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

        _backgroundTask = Task.Factory.StartNew(() =>
        {
            _ = ProcessChannelAsync(_channel.Reader, TimeSpan.FromSeconds(1), default);
        });
    }

    public ILogger CreateLogger(string name) => new LoggleLogger(name, _scopeProvider, _channel.Writer, _options.CurrentValue);

    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopeProvider = scopeProvider;

    private async Task ProcessChannelAsync(ChannelReader<LogMessageEntry> reader, TimeSpan flushInterval, CancellationToken cancellationToken)
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _egressOptions.CurrentValue.Kafka.BootstrapServers,
            SecurityProtocol = SecurityProtocol.SaslSsl,
            SaslMechanism = SaslMechanism.Plain,
            SaslUsername = "",
            SaslPassword = "",
            //ClientId = "ccloud-csharp-client-d3ca6def-bbcc-4924-8d99-ba206d1f6162"
        };

        using var producer = new ProducerBuilder<string, string>(producerConfig)
            .Build();

        try
        {
            var batch = new List<LogMessageEntry>();

            while (batch.Count < 100 && await reader.WaitToReadAsync(cancellationToken))
            {
                while (reader.TryRead(out var logMessage) && batch.Count < 2)
                {
                    batch.Add(logMessage);
                }
            }

            foreach (var item in batch)
            {
                await producer.ProduceAsync("demo-messages", new Message<string, string>()
                {
                    Key = Guid.NewGuid().ToString(),
                    Value = JsonSerializer.Serialize(item, SerializerOptions)
                });
            }
        }
        catch (Exception ex)
        {

            throw;
        }
    }

    public void Dispose()
    {
        _channel.Writer.Complete();
    }
}
