using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Bulk;
using Loggle.Web.Elasticsearch;
using Loggle.Web.Model;

namespace Loggle.Web;

public sealed class LogIngestionService
{
    private readonly ElasticsearchFactory _elasticsearchFactory;

    public LogIngestionService(ElasticsearchFactory elasticsearchFactory)
    {
        ThrowHelper.ThrowIfNull(elasticsearchFactory);

        _elasticsearchFactory = elasticsearchFactory;
    }

    public async Task<BulkResponse> IngestAsync(IEnumerable<OtlpLogEntry> logs, string dataStreamName, CancellationToken cancellationToken)
    {
        var bulkRequest = CreateIndexBulkRequest(logs, dataStreamName);

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

        bulkRequest.Operations = new BulkOperationsCollection(indexOps);
        bulkRequest.Refresh = Refresh.False;
        bulkRequest.RequireDataStream = true;

        return bulkRequest;
    }
}
