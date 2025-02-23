using System.Threading.Tasks;
using Loggle.Logging;
using Loggle.Web.Authentication.ApiKey;
using Loggle.Web.Elasticsearch;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Loggle.Web;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder
            .Logging
            .AddLoggleExporter(builder.Configuration);

        builder.Services.AddApiKey();
        builder.Services.AddElasticsearch();

        builder.Services.AddTransient<LogIngestionService>();

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
