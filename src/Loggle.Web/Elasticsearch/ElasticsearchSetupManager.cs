using System.Threading;
using System.Threading.Tasks;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Cluster;
using Elastic.Clients.Elasticsearch.IndexLifecycleManagement;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Transport;

namespace Loggle.Web.Elasticsearch;

public sealed class ElasticsearchSetupManager
{
    private readonly ElasticsearchClient _elasticsearchClient;

    public ElasticsearchSetupManager(ElasticsearchClient elasticsearchClient)
    {
        ThrowHelper.ThrowIfNull(elasticsearchClient);

        _elasticsearchClient = elasticsearchClient;
    }

    public async Task BootstrapElasticsearchAsync(CancellationToken cancellationToken)
    {
        var indexTemplate = GetDefaultIndexTeamplate();
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

    private (string Name, string JsonBody) GetDefaultIndexTeamplate()
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
