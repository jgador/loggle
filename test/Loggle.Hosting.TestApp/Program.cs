using Loggle.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Loggle.Hosting.TestApp;

public class Program
{
    public static async Task Main(string[] args)
    {
        // await RunAsync(args).ConfigureAwait(false);

        var buffer = new BufferedChannel<string>();
        var cts = new CancellationTokenSource();
        var writer = buffer.Writer;

        var consumerTask = buffer.ConsumeAsync(cts.Token);

        var producerTask = Task.Factory.StartNew(async () =>
        {
            int i = 0;

            while (!cts.Token.IsCancellationRequested)
            {
                var item = $"Item-{i}-payload";
                var success = writer.TryWrite(item);

                if (success)
                {
                    Console.WriteLine($"[Producer] Writing: {item}");
                }
                else
                {
                    Console.WriteLine("[Producer] Writing: false");
                }
                // await writer.WriteAsync(item, cts.Token);

                i++;

                // Simulate some irregular delay
                await Task.Delay(500, cts.Token);
            }
        },
        cts.Token,
        TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness,
        TaskScheduler.Default);

        await Task.Delay(TimeSpan.FromMinutes(1));
        Console.WriteLine("[Main] Cancelling...");
        cts.Cancel();

        await Task.WhenAll(producerTask, consumerTask);

        Console.WriteLine("All done!");
    }

    private static async Task RunAsync(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureLogging((context, factory) =>
            {
                factory.AddLoggle();
            })
            .ConfigureServices((context, services) =>
            {
                services.AddHostedService<LoggingBackgroundService>();
            });

        using var host = builder.Build();

        await host.RunAsync();
    }
}
