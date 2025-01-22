using System.Threading;
using System.Threading.Tasks;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Cluster;
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
        var ilm = GetDefaultIlmComponentTemplate();

        var success = await PutComponentTemplateAsync(ilm.Name, ilm.JsonBody, cancellationToken).ConfigureAwait(false);
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
                cancellationToken)
            .ConfigureAwait(false);

        return request.ApiCallDetails.HasSuccessfulStatusCode;
    }

    private (string Name, string JsonBody) GetDefaultComponentMappings()
    {
        const string Name = "logs_mapping";

        var jsonBody = @"{""template"":{""mappings"":{}}}";

        return (Name, jsonBody);
    }

    private (string Name, string JsonBody) GetDefaultIlmComponentTemplate()
    {
        const string Name = "rollover7d20gb_delete7d";
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
        const string Name = "logs_rollover7d20gb_delete7d";

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
