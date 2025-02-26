using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Loggle.Hosting.TestApp;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                // Register the loggle exporter
                services.AddLoggleExporter();
                services.AddHostedService<LoggingBackgroundService>();
                services.AddHostedService<YetAnotherLoggingBackgroundService>();
            });

        var host = builder.Build();

        await host.RunAsync();
    }
}
