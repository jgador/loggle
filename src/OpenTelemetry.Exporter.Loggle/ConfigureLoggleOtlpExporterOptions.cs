using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace OpenTelemetry.Exporter.Loggle
{
    public class ConfigureLoggleOtlpExporterOptions : IConfigureOptions<LoggleExporterOptions>
    {
        private readonly IConfiguration _configuration;

        public ConfigureLoggleOtlpExporterOptions(IConfiguration configuration)
        {
            ThrowHelper.ThrowIfNull(configuration);

            _configuration = configuration;
        }

        public void Configure(LoggleExporterOptions options)
            => _configuration.Bind(LoggleExporterOptions.SectionKey, options);
    }
}
