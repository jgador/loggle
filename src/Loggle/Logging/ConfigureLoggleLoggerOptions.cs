using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Loggle.Logging
{
    internal sealed class ConfigureLoggleLoggerOptions : IConfigureOptions<LoggleLoggerOptions>
    {
        private readonly IConfiguration _configuration;

        public ConfigureLoggleLoggerOptions(IConfiguration configuration)
        {
            ThrowHelper.ThrowIfNull(configuration);

            _configuration = configuration;
        }

        public void Configure(LoggleLoggerOptions options)
        {
            _configuration.Bind(LoggleLoggerOptions.SectionKey, options);
        }
    }
}
