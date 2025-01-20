using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Proto.Logs.V1;

namespace Loggle.Web.Model;

public class OtlpLogEntry
{
    public KeyValuePair<string, string>[] Attributes { get; }

    public DateTime TimeStamp { get; }

    public uint Flags { get; }

    public LogLevel Severity { get; }

    public string Message { get; }

    public string SpanId { get; }

    public string TraceId { get; }

    public string ParentId { get; }

    public string? OriginalFormat { get; }

    public OtlpLogEntry(LogRecord record, OtlpContext context)
    {
        TimeStamp = ResolveTimeStamp(record);

        string? originalFormat = null;
        string? parentId = null;

        Attributes = record.Attributes.ToKeyValuePairs(context, filter: attribute =>
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
