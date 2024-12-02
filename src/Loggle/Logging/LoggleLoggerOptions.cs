using Microsoft.Extensions.Logging;

namespace Loggle.Logging;

public class LoggleLoggerOptions
{
    public bool Enabled { get; set; } = true;

    public LogLevel? MinimumLevel { get; set; }

    public bool IncludeScopes { get; set; } = true;
}
