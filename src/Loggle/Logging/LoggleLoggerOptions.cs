using Loggle.Egress;
using Microsoft.Extensions.Logging;

namespace Loggle.Logging
{
    public class LoggleLoggerOptions
    {
        public const string SectionKey = "Logging:Loggle";

        public bool Enabled { get; set; } = true;

        public LogLevel? MinimumLevel { get; set; } = LogLevel.Information;

        public bool IncludeScopes { get; set; } = false;

        public EgressOptions? Egress { get; set; }
    }

}
