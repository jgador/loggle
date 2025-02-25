using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter.Loggle;

namespace Loggle.Web.Configuration;

public class ConfigureElasticsearchIngestOptions : IConfigureOptions<ElasticsearchIngestOptions>
{
    private readonly IConfiguration _configuration;

    public ConfigureElasticsearchIngestOptions(IConfiguration configuration)
    {
        ThrowHelper.ThrowIfNull(configuration);

        _configuration = configuration;
    }
    public void Configure(ElasticsearchIngestOptions options)
        => _configuration.Bind(ElasticsearchIngestOptions.SectionKey, options);
}
