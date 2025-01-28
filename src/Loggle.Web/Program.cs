using System;
using System.Threading.Tasks;
using Loggle.Web.Authentication.ApiKey;
using Loggle.Web.Elasticsearch;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;

namespace Loggle.Web;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder
            .Logging
            .AddOpenTelemetry(opt =>
            {
                opt.IncludeFormattedMessage = true;
                opt.ParseStateValues = true;
                opt.AddOtlpExporter(exporterOptions =>
                {
                    // exporterOptions.Endpoint = new Uri("http://localhost:4317");
                    // exporterOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;

                    exporterOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                    exporterOptions.Endpoint = new Uri("http://localhost:4318/v1/logs");

                    // When in docker
                    // exporterOptions.Endpoint = new Uri("http://host.docker.internal:4318/v1/logs");

                    exporterOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                });
            });

        builder.Services.AddApiKey();
        builder.Services.AddElasticsearch();
        builder.Services.AddElasticsearchV7();

        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseAuthentication();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
