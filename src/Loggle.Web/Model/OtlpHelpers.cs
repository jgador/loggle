using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Google.Protobuf;
using Google.Protobuf.Collections;
using OpenTelemetry.Proto.Common.V1;

namespace Loggle.Web.Model;

public static class OtlpHelpers
{
    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    public static string TruncateString(string value, int maxLength)
    {
        return value.Length > maxLength ? value[..maxLength] : value;
    }

    public static string ToHexString(ReadOnlyMemory<byte> bytes)
    {
        if (bytes.Length == 0)
        {
            return string.Empty;
        }

        // This produces lowercase hex string from the bytes. It's used instead of Convert.ToHexString()
        // because we want to display lowercase hex string in the UI for values such as traceid and spanid.
        return string.Create(bytes.Length * 2, bytes, static (chars, bytes) =>
        {
            var data = bytes.Span;
            for (var pos = 0; pos < data.Length; pos++)
            {
                ToCharsBuffer(data[pos], chars, pos * 2);
            }
        });
    }

    public static string ToHexString(this ByteString bytes)
    {
        return ToHexString(bytes.Memory);
    }

    public static string GetString(this AnyValue value) =>
        value.ValueCase switch
        {
            AnyValue.ValueOneofCase.StringValue => value.StringValue,
            AnyValue.ValueOneofCase.IntValue => value.IntValue.ToString(CultureInfo.InvariantCulture),
            AnyValue.ValueOneofCase.DoubleValue => value.DoubleValue.ToString(CultureInfo.InvariantCulture),
            AnyValue.ValueOneofCase.BoolValue => value.BoolValue ? "true" : "false",
            AnyValue.ValueOneofCase.BytesValue => value.BytesValue.ToHexString(),
            AnyValue.ValueOneofCase.ArrayValue => ConvertAnyValue(value)!.ToJsonString(s_jsonSerializerOptions),
            AnyValue.ValueOneofCase.KvlistValue => ConvertAnyValue(value)!.ToJsonString(s_jsonSerializerOptions),
            AnyValue.ValueOneofCase.None => string.Empty,
            _ => value.ToString(),
        };

    private static JsonNode? ConvertAnyValue(AnyValue value)
    {
        // Recursively convert AnyValue types to JsonNode types to produce more idiomatic JSON.
        // Recursing over incoming values is safe because Protobuf serializer imposes a safe limit on recursive messages.
        return value.ValueCase switch
        {
            AnyValue.ValueOneofCase.StringValue => JsonValue.Create(value.StringValue),
            AnyValue.ValueOneofCase.IntValue => JsonValue.Create(value.IntValue),
            AnyValue.ValueOneofCase.DoubleValue => JsonValue.Create(value.DoubleValue),
            AnyValue.ValueOneofCase.BoolValue => JsonValue.Create(value.BoolValue),
            AnyValue.ValueOneofCase.BytesValue => JsonValue.Create(value.BytesValue.ToHexString()),
            AnyValue.ValueOneofCase.ArrayValue => ConvertArray(value.ArrayValue),
            AnyValue.ValueOneofCase.KvlistValue => ConvertKeyValues(value.KvlistValue),
            AnyValue.ValueOneofCase.None => null,
            _ => throw new InvalidOperationException($"Unexpected AnyValue type: {value.ValueCase}"),
        };

        static JsonArray ConvertArray(ArrayValue value)
        {
            var a = new JsonArray();
            foreach (var item in value.Values)
            {
                a.Add(ConvertAnyValue(item));
            }
            return a;
        }

        static JsonObject ConvertKeyValues(KeyValueList value)
        {
            var o = new JsonObject();
            foreach (var item in value.Values)
            {
                o[item.Key] = ConvertAnyValue(item.Value);
            }
            return o;
        }
    }

    // From https://github.com/dotnet/runtime/blob/963954a11c1beeea4ad63002084a213d8d742864/src/libraries/Common/src/System/HexConverter.cs#L81-L89
    // Modified slightly to always produce lowercase output.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ToCharsBuffer(byte value, Span<char> buffer, int startingIndex = 0)
    {
        var difference = ((value & 0xF0U) << 4) + (value & 0x0FU) - 0x8989U;
        var packedResult = (((uint)-(int)difference & 0x7070U) >> 4) + difference + 0xB9B9U | 0x2020U;

        buffer[startingIndex + 1] = (char)(packedResult & 0xFF);
        buffer[startingIndex] = (char)(packedResult >> 8);
    }

    public static KeyValuePair<string, string>[] ToKeyValuePairs(this RepeatedField<KeyValue> attributes, OtlpContext context, Func<KeyValue, bool> filter)
    {
        if (attributes.Count == 0)
        {
            return [];
        }

        var readLimit = Math.Min(attributes.Count, context.Options.MaxAttributeCount);
        List<KeyValuePair<string, string>>? values = null;
        for (var i = 0; i < attributes.Count; i++)
        {
            var attribute = attributes[i];

            if (!filter(attribute))
            {
                continue;
            }

            values ??= new List<KeyValuePair<string, string>>(readLimit);

            var value = TruncateString(attribute.Value.GetString(), context.Options.MaxAttributeLength);

            // If there are duplicates then last value wins.
            var existingIndex = GetIndex(values, attribute.Key);
            if (existingIndex >= 0)
            {
                var existingAttribute = values[existingIndex];
                if (existingAttribute.Value != value)
                {
                    values[existingIndex] = new KeyValuePair<string, string>(attribute.Key, value);
                }
            }
            else
            {
                if (values.Count < readLimit)
                {
                    values.Add(new KeyValuePair<string, string>(attribute.Key, value));
                }
            }
        }

        return values?.ToArray() ?? [];

        static int GetIndex(List<KeyValuePair<string, string>> values, string name)
        {
            for (var i = 0; i < values.Count; i++)
            {
                if (values[i].Key == name)
                {
                    return i;
                }
            }
            return -1;
        }
    }

    public static DateTime UnixNanoSecondsToDateTime(ulong unixTimeNanoseconds)
    {
        var ticks = NanosecondsToTicks(unixTimeNanoseconds);

        return DateTime.UnixEpoch.AddTicks(ticks);
    }

    private static long NanosecondsToTicks(ulong nanoseconds)
    {
        return (long)(nanoseconds / TimeSpan.NanosecondsPerTick);
    }
}
