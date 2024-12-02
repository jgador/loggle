using Loggle.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Loggle.Hosting.TestApp;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = new HostBuilder()
            .ConfigureDefaults(args)
            .ConfigureLogging((_, factory) =>
            {
                factory.AddLoggle();
            });

        using var host = builder.Build();

        var config = host.Services.GetRequiredService<IConfiguration>();

        var providers = host.Services.GetServices<ILoggerProvider>();
    }
}
