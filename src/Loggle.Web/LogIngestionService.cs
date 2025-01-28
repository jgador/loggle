using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Bulk;
using Loggle.Web.Model;

namespace Loggle.Web;

public sealed class LogIngestionService
{
    private readonly ElasticsearchClient _elasticsearchClient;

    public LogIngestionService(ElasticsearchClient elasticsearchClient)
    {
        ThrowHelper.ThrowIfNull(elasticsearchClient);

        _elasticsearchClient = elasticsearchClient;
    }

    public async Task<BulkResponse> IngestAsync(IEnumerable<OtlpLogEntry> logs, string dataStreamName, CancellationToken cancellationToken)
    {
        var bulkRequest = CreateIndexBulkRequest(logs, dataStreamName);

        return await _elasticsearchClient.BulkAsync(bulkRequest, cancellationToken).ConfigureAwait(false);
    }

    private static BulkRequest CreateIndexBulkRequest<T>(IEnumerable<T> logs, string dataStreamName) where T : class
    {
        ThrowHelper.ThrowIfNull(logs);

        var bulkRequest = new BulkRequest(dataStreamName);

        var indexOps = logs
            .Select(o => new BulkCreateOperation<T>(o))
            .Cast<IBulkOperation>()
            .ToList();

        bulkRequest.Operations = new BulkOperationsCollection(indexOps);
        bulkRequest.Refresh = Refresh.False;
        bulkRequest.RequireDataStream = true;

        return bulkRequest;
    }
}
