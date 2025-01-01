using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;

namespace Loggle
{
    public class SystemTextJsonSerializer<T> : ISerializer<T>, IDeserializer<T>
    {
        private readonly JsonSerializerOptions _jsonOptions;
        public SystemTextJsonSerializer()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                IgnoreReadOnlyFields = true,
                IgnoreReadOnlyProperties = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                MaxDepth = 100
            };
        }

        public byte[] Serialize(T data, SerializationContext context)
        {
            var valueBytes = JsonSerializer.SerializeToUtf8Bytes(data, _jsonOptions);

            return valueBytes;
        }

        public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        {
            if (isNull)
            {
                return default;
            }

            var json = Encoding.UTF8.GetString(data);

            return JsonSerializer.Deserialize<T>(json, options: _jsonOptions);
        }
    }
}
