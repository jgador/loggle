using System.Diagnostics;

namespace Loggle;

public class LoggleOtlpExporterOptions
{
    public const string SectionKey = "Logging:Loggle";

    public string? BearerToken { get; set; } = string.Empty;

    public string? Endpoint { get; set; } = "http://localhost:4318/v1/logs";

    private string? _serviceName = null;
    public string ServiceName
    {
        get => _serviceName ?? GetDefaultServiceName();

        set => _serviceName = string.IsNullOrWhiteSpace(value)
            ? GetDefaultServiceName()
            : value;
    }

    public string ServiceVersion { get; set; } = "1.0.0";

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
