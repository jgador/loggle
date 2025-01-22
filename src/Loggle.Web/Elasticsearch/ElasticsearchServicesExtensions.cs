using System;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.DependencyInjection;

namespace Loggle.Web.Elasticsearch;

public static class ElasticsearchServicesExtensions
{
    public static IServiceCollection AddElasticsearch(this IServiceCollection services)
    {
        ThrowHelper.ThrowIfNull(services);

        // from docker compose
        const string hostName = "http://localhost:9200";
        var settings = new ElasticsearchClientSettings(new Uri(hostName));
        var client = new ElasticsearchClient(settings);

        services.AddSingleton(client);
        services.AddTransient<ElasticsearchSetupManager>();

        return services;
    }
}
