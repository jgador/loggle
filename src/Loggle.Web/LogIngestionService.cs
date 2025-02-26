using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Bulk;
using Loggle.Web.Configuration;
using Loggle.Web.Elasticsearch;
using Loggle.Web.Model;
using Microsoft.Extensions.Options;

namespace Loggle.Web;

public sealed class LogIngestionService
{
    private readonly ElasticsearchFactory _elasticsearchFactory;
    private readonly ElasticsearchIngestOptions _elasticsearchIngestOptions;

    public LogIngestionService(
        ElasticsearchFactory elasticsearchFactory,
        IOptionsMonitor<ElasticsearchIngestOptions> elasticsearchIngestOptions
    )
    {
        ThrowHelper.ThrowIfNull(elasticsearchFactory);
        ThrowHelper.ThrowIfNull(elasticsearchIngestOptions?.CurrentValue);

        _elasticsearchFactory = elasticsearchFactory;
        _elasticsearchIngestOptions = elasticsearchIngestOptions.CurrentValue;
    }

    public async Task<BulkResponse> IngestAsync(IEnumerable<OtlpLogEntry> logs, CancellationToken cancellationToken)
    {
        var bulkRequest = CreateIndexBulkRequest(logs, _elasticsearchIngestOptions.DataStreamName);

        var client = _elasticsearchFactory.CreateClient();

        return await client.BulkAsync(bulkRequest, cancellationToken).ConfigureAwait(false);
    }

    private static BulkRequest CreateIndexBulkRequest<T>(IEnumerable<T> logs, string dataStreamName) where T : class
    {
        ThrowHelper.ThrowIfNull(logs);

        var bulkRequest = new BulkRequest(dataStreamName);

        var indexOps = logs
            .Select(o => new BulkCreateOperation<T>(o))
            .Cast<IBulkOperation>()
            .ToList();

        bulkRequest.Operations = [.. indexOps];
        bulkRequest.Refresh = Refresh.False;
        bulkRequest.RequireDataStream = true;

        return bulkRequest;
    }
}
