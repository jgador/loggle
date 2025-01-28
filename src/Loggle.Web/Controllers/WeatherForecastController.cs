using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Loggle.Web.Elasticsearch;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Loggle.Web.Controllers;

[ApiController]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;
    private readonly ElasticsearchSetupManager _elasticsearchSetupManager;

    public WeatherForecastController(
        ILogger<WeatherForecastController> logger,
        ElasticsearchSetupManager elasticsearchSetupManager)
    {
        _logger = logger;
        _elasticsearchSetupManager = elasticsearchSetupManager;
    }

    [AllowAnonymous]
    [HttpGet]
    [Route("api/v1/weatherforecast")]
    public async Task<IEnumerable<WeatherForecast>> GetAsync(CancellationToken cancellationToken)
    {
        // Test bootstrap
        // await _elasticsearchSetupManager.BootstrapElasticsearchAsync(cancellationToken).ConfigureAwait(false);

        var result = Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();

        _logger.LogInformation(string.Join(",", result.Select(r => r.Summary).ToList()));

        return result;
    }
}
