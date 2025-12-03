using System;
using System.Collections.Generic;
using System.Linq;
using OpenTelemetry.Proto.Common.V1;

namespace Loggle.Web.Model;

public sealed class OtlpScope
{
    private const string UnknownScopeName = "unknown";

    public static readonly OtlpScope Empty = new(UnknownScopeName, string.Empty, Array.Empty<KeyValuePair<string, string>>());

    public string Name { get; }
    public string Version { get; }
    public KeyValuePair<string, string>[] Attributes { get; }

    private OtlpScope()
    {
        Name = UnknownScopeName;
        Version = string.Empty;
        Attributes = Array.Empty<KeyValuePair<string, string>>();
    }

    public OtlpScope(string? name, string? version, IEnumerable<KeyValuePair<string, string>>? attributes)
    {
        Name = string.IsNullOrWhiteSpace(name) ? UnknownScopeName : name!;
        Version = version ?? string.Empty;
        Attributes = attributes?.ToArray() ?? Array.Empty<KeyValuePair<string, string>>();
    }

    public OtlpScope(InstrumentationScope scope, OtlpContext context)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(context);

        Name = string.IsNullOrWhiteSpace(scope.Name) ? UnknownScopeName : scope.Name;
        Version = scope.Version ?? string.Empty;
        Attributes = scope.Attributes.ToKeyValuePairs(context, static _ => true);
    }
}
