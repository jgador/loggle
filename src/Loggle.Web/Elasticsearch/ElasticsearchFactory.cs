using System;
using Elastic.Clients.Elasticsearch;
using Loggle.Web.Configuration;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter.Loggle;

namespace Loggle.Web.Elasticsearch;

public class ElasticsearchFactory
{
    private IOptionsMonitor<ElasticsearchIngestOptions> _ingestOptions;

    public ElasticsearchFactory(IOptionsMonitor<ElasticsearchIngestOptions> ingestOptions)
    {
        ThrowHelper.ThrowIfNull(ingestOptions?.CurrentValue?.ElasticsearchIngestUrl);

        _ingestOptions = ingestOptions;
    }

    public ElasticsearchClient CreateClient()
    {
        var settings = new ElasticsearchClientSettings(new Uri(_ingestOptions!.CurrentValue!.ElasticsearchIngestUrl!))
            .DisablePing()
            .DisableAuditTrail()
            .DisableDirectStreaming();

        var client = new ElasticsearchClient(settings);

        return client;
    }
}
