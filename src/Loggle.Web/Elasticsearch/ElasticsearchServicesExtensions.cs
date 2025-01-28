using System;
using Elastic.Clients.Elasticsearch;
using Elasticsearch.Net;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using Nest.JsonNetSerializer;

namespace Loggle.Web.Elasticsearch;

public static class ElasticsearchServicesExtensions
{
    public static IServiceCollection AddElasticsearch(this IServiceCollection services)
    {
        ThrowHelper.ThrowIfNull(services);

        // from docker compose
        const string hostName = "http://localhost:9200";
        var settings = new ElasticsearchClientSettings(new Uri(hostName))
            .DisableDirectStreaming();

        var client = new ElasticsearchClient(settings);

        services.AddSingleton(client);
        services.AddTransient<ElasticsearchSetupManager>();

        return services;
    }

    public static IServiceCollection AddElasticsearchV7(this IServiceCollection services)
    {
        ThrowHelper.ThrowIfNull(services);

        // from docker compose
        const string hostName = "http://localhost:9200";
        var pool = new StaticConnectionPool(new[] { new Uri(hostName) });

        var settings = new ConnectionSettings(pool, sourceSerializer: JsonNetSerializer.Default)
            .DisablePing(true)
            .EnableHttpCompression(true)
            .IncludeServerStackTraceOnError(true)
            .RequestTimeout(TimeSpan.FromMinutes(1))
            .MaximumRetries(1)
            .ServerCertificateValidationCallback((o, certificate, arg3, arg4) => true)
            .DefaultDisableIdInference()
            .DefaultFieldNameInferrer(p => p)
            .EnableApiVersioningHeader(true); // enable compatibility mode

        var client = new ElasticClient(settings);

        services.AddSingleton(client);

        return services;
    }
}
