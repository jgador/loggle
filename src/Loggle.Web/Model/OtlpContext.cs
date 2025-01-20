using Loggle.Web.Configuration;

namespace Loggle.Web.Model;

public class OtlpContext
{
    public required TelemetryLimitOptions Options { get; init; }
}
