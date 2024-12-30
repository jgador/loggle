using Loggle.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Loggle.Hosting.TestApp;

public class Program
{
    public static async Task Main(string[] args)
    {
        var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (object? sender, ConsoleCancelEventArgs e) =>
        {
            cts.Cancel();
            e.Cancel = true;
        };

        var options = new BufferedChannelOptions()
        {
            MaxSize = 10,
            MaxLifetime = TimeSpan.FromSeconds(2)
        };

        var buffer = new BufferedChannel<string>(options, FlushBatchHandlerAsync);

        var writer = buffer.Writer;

        var consumerTask = buffer.ConsumeAsync();

        var producerTask = Task.Run(async () =>
        {
            int i = 1;

            while (!cts.Token.IsCancellationRequested)
            {
                var item = $"{i}";
                var success = writer.TryWrite(item);

                if (success)
                {
                    Console.WriteLine($"[Producer] Writing: {item}");
                }
                else
                {
                    Console.WriteLine($"[Producer] Writing: {success}");
                }

                i++;

                // Simulate delay
                await Task.Delay(Random.Shared.Next(60, 200), cts.Token);
            }
        });

        await Task.Delay(TimeSpan.FromSeconds(77));

        var completed = writer.TryComplete();

        Console.WriteLine($"Completed: {completed}.");

        await producerTask.ConfigureAwait(false);
        await consumerTask.ConfigureAwait(false);

        Console.WriteLine("All done!");
    }

    private static ValueTask FlushBatchHandlerAsync(IReadOnlyList<string> batch)
    {
        if (batch.Count == 0)
            return ValueTask.CompletedTask;

        Console.WriteLine($"[Consumer] Flushing {batch.Count} items");

        foreach (var item in batch)
        {
            Console.Write($" {item}");
        }

        Console.WriteLine();

        return ValueTask.CompletedTask;
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
