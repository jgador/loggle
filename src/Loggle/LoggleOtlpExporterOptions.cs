using System.Diagnostics;

namespace Loggle;

public class LoggleOtlpExporterOptions
{
    public const string SectionKey = "Logging:Loggle";

    private string? _serviceName = null;
    public string ServiceName
    {
        get => _serviceName ?? GetDefaultServiceName();

        set => _serviceName = string.IsNullOrWhiteSpace(value)
            ? GetDefaultServiceName()
            : value;
    }

    public string ServiceVersion { get; set; } = "1.0.0";

    public OtelCollectorOptions? OtelCollector { get; set; } = new();

    private string GetDefaultServiceName()
    {
        var defaultServiceName = "unknown_service";

        try
        {
            var processName = Process.GetCurrentProcess().ProcessName;
            if (!string.IsNullOrWhiteSpace(processName))
            {
                defaultServiceName = $"{defaultServiceName}:{processName}";
            }
        }
        catch
        {
            // GetCurrentProcess can throw PlatformNotSupportedException
        }

        return defaultServiceName;
    }
}
