using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Loggle.Web.Model;
using Nest;
using PutComponentTemplateResponse = Elastic.Clients.Elasticsearch.Cluster.PutComponentTemplateResponse;
using PutIndexTemplateResponse = Elastic.Clients.Elasticsearch.IndexManagement.PutIndexTemplateResponse;
using PutLifecycleResponse = Elastic.Clients.Elasticsearch.IndexLifecycleManagement.PutLifecycleResponse;

namespace Loggle.Web.Elasticsearch;

public sealed class ElasticsearchSetupManager
{
    private readonly ElasticsearchClient _elasticsearchClient;
    private readonly ElasticClient _nestClient;

    public ElasticsearchSetupManager(ElasticsearchClient elasticsearchClient, ElasticClient nestClient)
    {
        ThrowHelper.ThrowIfNull(elasticsearchClient);
        ThrowHelper.ThrowIfNull(nestClient);

        _elasticsearchClient = elasticsearchClient;
        _nestClient = nestClient;
    }

    public async Task BootstrapElasticsearchAsync(CancellationToken cancellationToken)
    {
        var indexTemplate = GetDefaultIndexTemplate();
        var ilm = GetDefaultIlmComponentTemplate();
        var mapping = GetDefaultComponentMappings();

        if (await IndexTemplateExistsAsync(indexTemplate.Name, cancellationToken).ConfigureAwait(false)) return;

        // Default ILM policy
        _ = await PutIlmPolicyAsync(cancellationToken).ConfigureAwait(false);

        // Default ILM policy component template
        _ = await PutComponentTemplateAsync(ilm.Name, ilm.JsonBody, cancellationToken).ConfigureAwait(false);

        // Default mapping component template
        _ = await PutComponentTemplateAsync(mapping.Name, mapping.JsonBody, cancellationToken).ConfigureAwait(false);

        // Default index template
        _ = await PutIndexTemplateAsync(indexTemplate.Name, indexTemplate.JsonBody, cancellationToken).ConfigureAwait(false);

        // Default data stream
        _ = await PutDataStreamAsync(indexTemplate.Name, cancellationToken).ConfigureAwait(false);

        await CreateMappingAsync(indexTemplate.Name);
    }

    public async Task<bool> DataStreamExistsAsync(string dataStreamName, CancellationToken cancellationToken)
    {
        var response = await _elasticsearchClient
            .Indices
            .GetDataStreamAsync(dataStreamName, cancellationToken)
            .ConfigureAwait(false);

        return response.ApiCallDetails is { HasSuccessfulStatusCode: true, HttpStatusCode: (int)HttpStatusCode.OK };
    }

    public async Task<bool> UpdateMappingAsync(string dataStreamName)
    {
        return await CreateMappingAsync(dataStreamName).ConfigureAwait(false);
    }

    public string GetDefaultDataStreamName() => GetDefaultIndexTemplate().Name;

    private async Task<bool> CreateMappingAsync(string dataStreamName)
    {
        var response = await _nestClient
            .MapAsync<OtlpLogEntry>(m => m
                .RequestConfiguration(c => c.DisableDirectStreaming(disable: true))
                .Index(dataStreamName)
                .AutoMap()
            ).ConfigureAwait(false);

        return response.ApiCall.Success;
    }

    private async Task<bool> PutIlmPolicyAsync(CancellationToken cancellation = default)
    {
        var ilm = GetDefaultIlmPolicy();
        var path = $"_ilm/policy/{ilm.Name}";

        var request = await _elasticsearchClient
            .Transport
            .RequestAsync<PutLifecycleResponse>(
                HttpMethod.PUT,
                path,
                PostData.String(ilm.JsonBody),
                cancellation
            ).ConfigureAwait(false);

        return request.ApiCallDetails.HasSuccessfulStatusCode;
    }

    private async Task<bool> IndexTemplateExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        var response = await _elasticsearchClient
            .Indices
            .ExistsIndexTemplateAsync(name, cancellationToken).ConfigureAwait(false);

        return response.Exists;
    }

    private async Task<bool> PutDataStreamAsync(string name, CancellationToken cancellationToken = default)
    {
        var getResponse = await _elasticsearchClient
            .Indices
            .GetDataStreamAsync(name, cancellationToken)
            .ConfigureAwait(false);

        if (getResponse.ApiCallDetails is { HasSuccessfulStatusCode: true, HttpStatusCode: (int)HttpStatusCode.NotFound })
        {
            var response = await _elasticsearchClient
                .Indices
                .CreateDataStreamAsync(name, cancellationToken)
                .ConfigureAwait(false);

            return response.ApiCallDetails.HasSuccessfulStatusCode;
        }

        return true;
    }

    private async Task<bool> PutIndexTemplateAsync(string name, string body, CancellationToken cancellationToken = default)
    {
        var path = $"_index_template/{name}";

        var response = await _elasticsearchClient
            .Transport
            .RequestAsync<PutIndexTemplateResponse>(
                HttpMethod.PUT,
                path,
                PostData.String(body),
                cancellationToken
            ).ConfigureAwait(false);

        return true;
    }

    private async Task<bool> PutComponentTemplateAsync(string name, string body, CancellationToken cancellationToken = default)
    {
        var path = $"_component_template/{name}";

        var request = await _elasticsearchClient
            .Transport
            .RequestAsync<PutComponentTemplateResponse>(
                HttpMethod.PUT,
                path,
                PostData.String(body),
                cancellationToken
            ).ConfigureAwait(false);

        return request.ApiCallDetails.HasSuccessfulStatusCode;
    }

    private (string Name, string JsonBody) GetDefaultIndexTemplate()
    {
        const string Name = "logs-loggle-default"; // convention: type-dataset-namespace
        var ilm = GetDefaultIlmComponentTemplate();
        var mapping = GetDefaultComponentMappings();

        var jsonBody = $$"""
        {
            "composed_of": [
              "{{ilm.Name}}",
              "{{mapping.Name}}"
            ],
            "data_stream": {},
            "index_patterns": ["{{Name}}"],
            "priority": 201
        }
        """;

        return (Name, jsonBody);
    }

    private (string Name, string JsonBody) GetDefaultComponentMappings()
    {
        const string Name = "loggle_mapping";

        var jsonBody = @"{""template"":{""mappings"":{}}}";

        return (Name, jsonBody);
    }

    private (string Name, string JsonBody) GetDefaultIlmComponentTemplate()
    {
        const string Name = "loggle_default_7d";
        var ilm = GetDefaultIlmPolicy();

        var jsonBody = $$"""
        {
            "template": {
                "settings": {
                    "index.lifecycle.name": "{{ilm.Name}}"
                }
            }
        }
        """;

        return (Name, jsonBody);
    }

    private (string Name, string JsonBody) GetDefaultIlmPolicy()
    {
        const string Name = "loggle_default_7d";

        // The min_age in the delete phase means delete the index 7 days after the rollover
        var jsonBody = """
        {
          "policy": {
            "phases": {
              "delete": {
                "actions": {
                  "delete": {
                  }
                },
                "min_age": "7d"
              },
              "hot": {
                "actions": {
                  "rollover": {
                    "max_age": "7d",
                    "max_size": "20gb"
                  },
                  "set_priority": {
                    "priority": 100
                  }
                },
                "min_age": "0ms"
              }
            }
          }
        }
        """;

        return (Name, jsonBody);
    }
}
