using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

namespace Loggle
{
    public static class LoggleExporterExtensions
    {
        public static OpenTelemetryBuilder AddLoggleExporter(
            this IServiceCollection services,
            Action<OtlpExporterOptions, LogRecordExportProcessorOptions>? configureExporterAndProcessor = null)
        {
            services.AddTransient<IConfigureOptions<LoggleExporterOptions>, ConfigureLoggleOtlpExporterOptions>();

            // Not using options name
            var optionsName = Options.DefaultName;
            LoggleExporterOptions loggleExportOptions = new();

            var builder = services.AddOpenTelemetry()
                .WithLogging(builder =>
                {
                    builder.AddProcessor(sp =>
                    {
                        var exporterOptions = sp.GetRequiredService<IOptionsMonitor<OtlpExporterOptions>>().Get(optionsName);
                        var processorOptions = sp.GetRequiredService<IOptionsMonitor<LogRecordExportProcessorOptions>>().Get(optionsName);
                        loggleExportOptions = sp.GetRequiredService<IOptionsMonitor<LoggleExporterOptions>>().Get(optionsName);

                        // Currently fixed to only use HttpProtobuf
                        exporterOptions.Protocol = OtlpExportProtocol.HttpProtobuf;
                        exporterOptions.Endpoint = new Uri(loggleExportOptions?.OtelCollector!.LogsReceiverEndpoint!);
                        exporterOptions.HttpClientFactory = () =>
                        {
                            var client = new HttpClient();

                            client.DefaultRequestHeaders.Authorization = new("Bearer", loggleExportOptions?.OtelCollector?.BearerToken!);

                            return client;
                        };

                        configureExporterAndProcessor?.Invoke(exporterOptions, processorOptions);

                        return BuildOtlpLogExporter(
                            sp,
                            exporterOptions,
                            processorOptions);
                    });
                })
                .ConfigureResource(r => r
                    .AddService(
                        serviceName: loggleExportOptions.ServiceName,
                        serviceVersion: loggleExportOptions.ServiceVersion,
                        serviceInstanceId: Environment.MachineName)
                );

            return builder;
        }

        internal static BaseProcessor<LogRecord> BuildOtlpLogExporter(
            IServiceProvider serviceProvider,
            OtlpExporterOptions exporterOptions,
            LogRecordExportProcessorOptions processorOptions)
        {
            BaseExporter<LogRecord> otlpExporter = new OtlpLogExporter(
                exporterOptions!);

            var batchOptions = processorOptions.BatchExportProcessorOptions;

            return new BatchLogRecordExportProcessor(
                otlpExporter,
                batchOptions.MaxQueueSize,
                batchOptions.ScheduledDelayMilliseconds,
                batchOptions.ExporterTimeoutMilliseconds,
                batchOptions.MaxExportBatchSize);
        }
    }
}
