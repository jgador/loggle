using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Bulk;
using Loggle.Web.Configuration;
using Loggle.Web.Elasticsearch;
using Loggle.Web.Model;
using Microsoft.Extensions.Options;
using OpenTelemetry.Proto.Collector.Logs.V1;

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

    public async Task<ExportLogsServiceResponse> IngestAsync(ExportLogsServiceRequest request, CancellationToken cancellationToken)
    {
        int failureCount = 0;

        var context = new OtlpContext
        {
            Options = new TelemetryLimitOptions { }
        };

        Debug.Assert(request.ResourceLogs.Count < 2, "Batched logs from the collector usually contain a single resource log per application; multiple entries are unexpected.");

        foreach (var rl in request.ResourceLogs)
        {
            var application = new OtlpApplication(context, rl.Resource);

            foreach (var sl in rl.ScopeLogs)
            {
                var logs = new List<OtlpLogEntry>(sl.LogRecords.Count);
                foreach (var record in sl.LogRecords)
                {
                    var logEntry = new OtlpLogEntry(context, application, record);
                    logs.Add(logEntry);
                }

                var bulkRequest = CreateIndexBulkRequest(logs, _elasticsearchIngestOptions.DataStreamName);
                var client = _elasticsearchFactory.CreateClient();
                var response = await client.BulkAsync(bulkRequest, cancellationToken).ConfigureAwait(false);

                failureCount += logs.Count - (response?.Items?.Count ?? 0);
            }
        }

        return new ExportLogsServiceResponse
        {
            PartialSuccess = new ExportLogsPartialSuccess
            {
                RejectedLogRecords = failureCount
            }
        };
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
