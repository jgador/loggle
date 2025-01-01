using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Loggle.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Loggle.Hosting.TestApp;

public class Program
{
    public static async Task Main(string[] args)
    {
        // await SimulateProducerConsumerAsync().ConfigureAwait(false);
        await RunAsync(args).ConfigureAwait(false);
    }

    private static async Task SimulateProducerConsumerAsync()
    {
        var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (object? sender, ConsoleCancelEventArgs e) =>
        {
            cts.Cancel();
            e.Cancel = true;
        };

        var options = new BufferedChannelOptions()
        {
            MaxSize = 15,
            MaxLifetime = TimeSpan.FromSeconds(2)
        };

        var channel = new BufferedChannel<string>(options, FlushBatchHandlerAsync);

        var writer = channel.Writer;

        var consumerTask = channel.ConsumeAsync();

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
                try
                {
                    await Task.Delay(Random.Shared.Next(56, 789), cts.Token);
                }
                catch { }
            }
        });

        await Task.Delay(TimeSpan.FromSeconds(10));

        var completed = writer.TryComplete();

        Console.WriteLine($"Completed: {completed}.");

        await producerTask.ConfigureAwait(false);
        await consumerTask.ConfigureAwait(false);

        Console.WriteLine("All done!");
    }

    private static async ValueTask SendToKafkaAsync(IReadOnlyList<string> batch)
    {
        try
        {
            const string BootstrapServers = "localhost:9092";
            const string TopicName = "wsl-demo-topic";

            var config = new AdminClientConfig
            {
                BootstrapServers = BootstrapServers
            };

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = BootstrapServers,
                SecurityProtocol = SecurityProtocol.Plaintext,
                SaslMechanism = SaslMechanism.Plain
            };

            using var producer = new ProducerBuilder<string, string>(producerConfig)
               .Build();

            foreach (var item in batch)
            {
                var result = await producer.ProduceAsync(TopicName, new Message<string, string>
                {
                    Key = item,
                    Value = item
                });

                Console.WriteLine($"Message produced to {result.TopicPartitionOffset}");
            }
        }
        catch (Exception ex)
        {
        }
    }

    private static ValueTask FlushBatchHandlerAsync(IReadOnlyList<string> batch)
    {
        if (batch.Count == 0)
            return ValueTask.CompletedTask;

        Console.WriteLine($"[Consumer] Flushing {batch.Count} item(s): {string.Join(",", batch)}");

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
