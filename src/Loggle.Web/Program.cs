using Loggle.Web;
using Loggle.Web.Authentication.ApiKey;
using Loggle.Web.Configuration;
using Loggle.Web.Elasticsearch;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiKey();
builder.Services.AddSingleton<ElasticsearchFactory>();
builder.Services.AddTransient<LogIngestionService>();
builder.Services.AddTransient<IConfigureOptions<ElasticsearchIngestOptions>, ConfigureElasticsearchIngestOptions>();

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

await app.RunAsync().ConfigureAwait(false);
