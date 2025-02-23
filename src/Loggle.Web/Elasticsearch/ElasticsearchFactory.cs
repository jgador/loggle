using System;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Options;

namespace Loggle.Web.Elasticsearch;

public class ElasticsearchFactory
{
    private IOptionsMonitor<LoggleOtlpExporterOptions> _loggleOtlpExporterOptions;

    public ElasticsearchFactory(IOptionsMonitor<LoggleOtlpExporterOptions> loggleOtlpExporterOptions)
    {
        ThrowHelper.ThrowIfNull(loggleOtlpExporterOptions);

        _loggleOtlpExporterOptions = loggleOtlpExporterOptions;
    }

    public ElasticsearchClient CreateClient()
    {
        var settings = new ElasticsearchClientSettings(new Uri(_loggleOtlpExporterOptions.CurrentValue.ElasticsearchIngestUrl))
            .DisableDirectStreaming();

        // TODO: cache the client
        var client = new ElasticsearchClient(settings);

        return client;
    }
}
