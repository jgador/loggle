using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Proto.Logs.V1;

namespace Loggle.Web.Model;

public class OtlpLogEntry
{
    [JsonPropertyName("@timestamp")]
    public DateTime TimeStamp { get; }

    [JsonPropertyName("serviceAttributes")]
    public List<NameValue> ServiceAttributes { get; set; }

    [JsonPropertyName("attributes")]
    public List<NameValue> Attributes { get; }

    [JsonPropertyName("serviceName")]
    public string ServiceName { get; set; }

    [JsonPropertyName("serviceInstanceId")]
    public string ServiceInstanceId { get; set; }

    [JsonPropertyName("serviceVersion")]
    public string ServiceVersion { get; set; }

    [JsonPropertyName("flags")]
    public uint Flags { get; }

    [JsonPropertyName("logLevel")]
    public LogLevel Severity { get; }

    [JsonPropertyName("message")]
    public string Message { get; }

    [JsonPropertyName("spanId")]
    public string SpanId { get; }

    [JsonPropertyName("traceId")]
    public string TraceId { get; }

    [JsonPropertyName("parentId")]
    public string ParentId { get; }

    [JsonPropertyName("originalFormat")]
    public string? OriginalFormat { get; }

    public OtlpLogEntry(OtlpContext context, OtlpApplication application, LogRecord record)
    {
        TimeStamp = ResolveTimeStamp(record);

        string? originalFormat = null;
        string? parentId = null;

        var attributes = record.Attributes.ToKeyValuePairs(context, filter: attribute =>
        {
            switch (attribute.Key)
            {
                case "{OriginalFormat}":
                    originalFormat = attribute.Value.GetString();
                    return false;
                case "ParentId":
                    parentId = attribute.Value.GetString();
                    return false;
                case "SpanId":
                case "TraceId":
                    // Explicitly ignore these
                    return false;
                default:
                    return true;
            }
        });

        ServiceName = application.ServiceName;
        ServiceVersion = application.ServiceVersionNumber;
        ServiceInstanceId = application.ServiceInstanceId;

        Attributes = attributes
            ?.Select(a => new NameValue { Name = a.Key, Value = a.Value })
            ?.ToList() ?? [];

        ServiceAttributes = application
            ?.Properties
            ?.Select(p => new NameValue { Name = p.Key, Value = p.Value })
            ?.ToList() ?? [];

        Flags = record.Flags;
        Severity = MapSeverity(record.SeverityNumber);

        Message = record.Body is { } body
            ? OtlpHelpers.TruncateString(body.GetString(), context.Options.MaxAttributeLength)
            : string.Empty;

        OriginalFormat = originalFormat;
        SpanId = record.SpanId.ToHexString();
        TraceId = record.TraceId.ToHexString();
        ParentId = parentId ?? string.Empty;
    }

    private static DateTime ResolveTimeStamp(LogRecord record)
    {
        // From proto docs:
        //
        // For converting OpenTelemetry log data to formats that support only one timestamp or
        // when receiving OpenTelemetry log data by recipients that support only one timestamp
        // internally the following logic is recommended:
        //   - Use time_unix_nano if it is present, otherwise use observed_time_unix_nano.
        var resolvedTimeUnixNano = record.TimeUnixNano != 0 ? record.TimeUnixNano : record.ObservedTimeUnixNano;

        return OtlpHelpers.UnixNanoSecondsToDateTime(resolvedTimeUnixNano);
    }

    private static LogLevel MapSeverity(SeverityNumber severityNumber) => severityNumber switch
    {
        SeverityNumber.Trace => LogLevel.Trace,
        SeverityNumber.Trace2 => LogLevel.Trace,
        SeverityNumber.Trace3 => LogLevel.Trace,
        SeverityNumber.Trace4 => LogLevel.Trace,
        SeverityNumber.Debug => LogLevel.Debug,
        SeverityNumber.Debug2 => LogLevel.Debug,
        SeverityNumber.Debug3 => LogLevel.Debug,
        SeverityNumber.Debug4 => LogLevel.Debug,
        SeverityNumber.Info => LogLevel.Information,
        SeverityNumber.Info2 => LogLevel.Information,
        SeverityNumber.Info3 => LogLevel.Information,
        SeverityNumber.Info4 => LogLevel.Information,
        SeverityNumber.Warn => LogLevel.Warning,
        SeverityNumber.Warn2 => LogLevel.Warning,
        SeverityNumber.Warn3 => LogLevel.Warning,
        SeverityNumber.Warn4 => LogLevel.Warning,
        SeverityNumber.Error => LogLevel.Error,
        SeverityNumber.Error2 => LogLevel.Error,
        SeverityNumber.Error3 => LogLevel.Error,
        SeverityNumber.Error4 => LogLevel.Error,
        SeverityNumber.Fatal => LogLevel.Critical,
        SeverityNumber.Fatal2 => LogLevel.Critical,
        SeverityNumber.Fatal3 => LogLevel.Critical,
        SeverityNumber.Fatal4 => LogLevel.Critical,
        _ => LogLevel.None
    };
}
