using Loggle.Egress;
using Loggle.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Loggle.Hosting.TestApp;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureLogging((context, factory) =>
            {
                factory.AddLoggle();
            });

        using var host = builder.Build();

        var config = host.Services.GetRequiredService<IConfiguration>();

        var egressOptions = host.Services.GetRequiredService<IOptionsMonitor<EgressOptions>>();
        var egress = egressOptions.CurrentValue;

        await host.RunAsync();
    }
}
