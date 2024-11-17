
using System.Diagnostics;
using Docker.DotNet;
using Docker.DotNet.Models;
using Newtonsoft.Json;

namespace Loggle.Tests;

public sealed class TestFixture : IAsyncLifetime, IDisposable
{
    private const string KafkaImage = "apache/kafka:3.9.0";
    public DockerClientConfiguration DockerClientConfiguration { get; }
    public DockerClient DockerClient { get; }
    public CancellationTokenSource Cts { get; }

    public TestFixture()
    {
        DockerClientConfiguration = new();
        DockerClient = DockerClientConfiguration.CreateClient();
        Cts = new(TimeSpan.FromMinutes(5));
        Cts.Token.Register(() => throw new TimeoutException());
    }

    public async Task InitializeAsync()
    {
        await DockerClient
            .Images
            .CreateImageAsync(new ImagesCreateParameters
            {
                FromImage = KafkaImage
            },
            authConfig: null,
            progress: WriteProgressOutput,
            Cts.Token).ConfigureAwait(false);
    }

    public void Dispose()
    {
        DockerClient.Dispose();
        DockerClientConfiguration.Dispose();
        Cts.Dispose();
    }
    public async Task DisposeAsync()
    {

    }

    private static Progress<JSONMessage> WriteProgressOutput =>
        new(jsonMessage =>
        {
            var message = JsonConvert.SerializeObject(jsonMessage);
            Console.WriteLine(message);
            Debug.WriteLine(message);
        });
}
