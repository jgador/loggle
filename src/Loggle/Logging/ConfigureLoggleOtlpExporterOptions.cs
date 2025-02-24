using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Loggle.Logging
{
    class ConfigureLoggleOtlpExporterOptions : IConfigureOptions<LoggleOtlpExporterOptions>
    {
        private readonly IConfiguration _configuration;

        public ConfigureLoggleOtlpExporterOptions(IConfiguration configuration)
        {
            ThrowHelper.ThrowIfNull(configuration);

            _configuration = configuration;
        }

        public void Configure(LoggleOtlpExporterOptions options)
            => _configuration.Bind(LoggleOtlpExporterOptions.SectionKey);
    }
}
