using System;
using System.Text;
using System.Text.Json;
using Google.Protobuf;
using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Proto.Resource.V1;
using Xunit.Abstractions;

namespace Loggle.Tests;

public class ProtoVsJsonSizeTests
{
    private readonly ITestOutputHelper _output;

    public ProtoVsJsonSizeTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void ProtobufEncodingProducesSmallerPayloadThanJson()
    {
        var request = CreateSampleExportRequest();

        var protobufBytes = request.ToByteArray();
        var jsonText = JsonFormatter.Default.Format(request);
        var jsonBytes = Encoding.UTF8.GetBytes(jsonText);
        using var jsonDoc = JsonDocument.Parse(jsonBytes);
        var jsonPretty = JsonSerializer.Serialize(jsonDoc.RootElement, new JsonSerializerOptions { WriteIndented = true });

        var protobufHex = BitConverter.ToString(protobufBytes).Replace("-", " ");

        _output.WriteLine("Serialized JSON payload (pretty):");
        _output.WriteLine(jsonPretty);
        _output.WriteLine(string.Empty);
        _output.WriteLine("Serialized Protobuf payload (bytes in hex):");
        _output.WriteLine(protobufHex);
        _output.WriteLine(string.Empty);
        _output.WriteLine("Protobuf byte length: {0}", protobufBytes.Length);
        _output.WriteLine("JSON byte length: {0}", jsonBytes.Length);

        Assert.True(
            protobufBytes.Length < jsonBytes.Length,
            $"Expected protobuf payload to be smaller than JSON, but protobuf was {protobufBytes.Length} bytes and JSON was {jsonBytes.Length} bytes.");
    }

    private static ExportLogsServiceRequest CreateSampleExportRequest()
    {
        var resourceLogs = new ResourceLogs
        {
            Resource = new Resource
            {
                Attributes =
                {
                    new KeyValue
                    {
                        Key = "service.name",
                        Value = new AnyValue { StringValue = "loggle-demo-service" }
                    },
                    new KeyValue
                    {
                        Key = "deployment.environment",
                        Value = new AnyValue { StringValue = "demo" }
                    }
                }
            }
        };

        var scopeLogs = new ScopeLogs
        {
            Scope = new InstrumentationScope
            {
                Name = "Loggle.Tests",
                Version = "1.0.0"
            }
        };

        scopeLogs.LogRecords.Add(new LogRecord
        {
            TimeUnixNano = 1_700_000_000_123_456_000,
            SeverityNumber = SeverityNumber.Info,
            SeverityText = "INFO",
            Body = new AnyValue { StringValue = "Simulated OTLP log record for size comparison." },
            Attributes =
            {
                new KeyValue { Key = "example", Value = new AnyValue { BoolValue = true } },
                new KeyValue { Key = "counter", Value = new AnyValue { IntValue = 7 } }
            }
        });

        resourceLogs.ScopeLogs.Add(scopeLogs);

        var request = new ExportLogsServiceRequest();
        request.ResourceLogs.Add(resourceLogs);

        return request;
    }
}
