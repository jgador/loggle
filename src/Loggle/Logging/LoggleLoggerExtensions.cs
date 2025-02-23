using System;
using System.Net.Http;
using Loggle.Egress;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

namespace Loggle.Logging;

public static class LoggleLoggerExtensions
{
    [Obsolete("Use AddOtlpLoggleExporter")]
    public static ILoggingBuilder AddLoggle(this ILoggingBuilder builder)
    {
        builder.Services.AddSingleton<ILoggerProvider, LoggleLoggerProvider>();
        builder.Services.AddTransient<IConfigureOptions<LoggleLoggerOptions>, ConfigureLoggleLoggerOptions>();
        builder.Services.AddSingleton<IEgressLoggerProcessor, KafkaEgressLoggerProcessor>(); // default for now

        return builder;
    }

    // TODO: Use something like the IConfigureOptions
    public static ILoggingBuilder AddLoggleExporter(this ILoggingBuilder builder, IConfiguration configuration)
    {
        var loggleConfig = configuration
            .GetSection(LoggleOtlpExporterOptions.SectionKey);

        var loggleExportOptions = loggleConfig.Get<LoggleOtlpExporterOptions>() ?? new();

        builder.Services.Configure<LoggleOtlpExporterOptions>(loggleConfig);

        builder.Services.AddLogging(builder =>
        {
            builder.AddOpenTelemetry(o =>
            {
                o.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(
                    serviceName: loggleExportOptions.ServiceName,
                    serviceVersion: loggleExportOptions.ServiceVersion));

                o.AddOtlpExporter((exporterOptions, processorOptions) =>
                {
                    processorOptions.ExportProcessorType = ExportProcessorType.Batch;
                    exporterOptions.Protocol = OtlpExportProtocol.HttpProtobuf;
                    exporterOptions.Endpoint = new Uri(loggleExportOptions.Endpoint!);

                    exporterOptions.HttpClientFactory = () =>
                    {
                        var client = new HttpClient();

                        client.DefaultRequestHeaders.Authorization = new("Bearer", loggleExportOptions?.BearerToken);

                        return client;
                    };
                });
            });
        });

        return builder;
    }
}
