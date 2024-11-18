using System.Diagnostics;
using System.Text.Json;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace Loggle.Tests;

public sealed class KafkaFixture : IAsyncLifetime, IDisposable
{
    private const string KafkaImage = "apache/kafka:3.9.0";
    private const string KafkaContainerName = "kafka";

    public readonly DockerClientConfiguration DockerClientConfiguration;
    public readonly DockerClient DockerClient;
    public readonly CancellationTokenSource Cts;
    public ImagesListResponse? Image { get; private set; }

    public KafkaFixture()
    {
        DockerClientConfiguration = new();
        DockerClient = DockerClientConfiguration.CreateClient();
        Cts = new(TimeSpan.FromMinutes(3));
        Cts.Token.Register(() => throw new TimeoutException("Kafka Docker image download timed out."));
    }

    public async Task InitializeAsync()
    {
        // Create image
        await DockerClient
            .Images
            .CreateImageAsync(new ImagesCreateParameters
            {
                FromImage = KafkaImage
            },
            authConfig: null,
            progress: WriteProgressOutput,
            Cts.Token).ConfigureAwait(false);

        var images = await GetKafkaImagesAsync(Cts.Token).ConfigureAwait(false);

        // Set image
        Image = images.Single();

        var kafkaContainers = await GetKafkaContainersAsync(Cts.Token).ConfigureAwait(false);

        // Create & start container if it's not existing
        if (kafkaContainers is null || kafkaContainers.Count == 0)
        {
            // Create container
            var createContainerResponse = await DockerClient
                .Containers
                .CreateContainerAsync(new CreateContainerParameters
                {
                    Image = Image.ID,
                    Name = KafkaContainerName
                }, Cts.Token
                ).ConfigureAwait(false);

            // Start container
            await DockerClient
                .Containers
                .StartContainerAsync(
                   createContainerResponse.ID,
                   new ContainerStartParameters(),
                   Cts.Token
                ).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        DockerClient?.Dispose();
        DockerClientConfiguration?.Dispose();
        Cts?.Dispose();
    }

    public async Task DisposeAsync()
    {
        var containers = await GetKafkaContainersAsync(Cts.Token).ConfigureAwait(false);
        var images = await GetKafkaImagesAsync(Cts.Token).ConfigureAwait (false);

        foreach (var container in containers)
        {
            await DockerClient
                .Containers
                .RemoveContainerAsync(
                    container.ID,
                    new ContainerRemoveParameters
                    {
                        Force = true
                    },
                    Cts.Token
                ).ConfigureAwait(false);
        }

        foreach (var image in images)
        {
            await DockerClient
                .Images
                .DeleteImageAsync(
                    image.ID,
                    new ImageDeleteParameters
                    {
                        Force = true
                    },
                    Cts.Token
                ).ConfigureAwait(false);
        }
    }

    private static Progress<JSONMessage> WriteProgressOutput =>
        new(jsonMessage =>
        {
            var message = JsonSerializer.Serialize(jsonMessage);
            Console.WriteLine(message);
            Debug.WriteLine(message);
        });

    private async Task<IList<ImagesListResponse>> GetKafkaImagesAsync(CancellationToken cancellationToken)
    {
        var images = await DockerClient
            .Images
            .ListImagesAsync(new ImagesListParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    ["reference"] = new Dictionary<string, bool>
                    {
                        [KafkaImage] = true
                    }
                }
            },
            cancellationToken).ConfigureAwait(false);

        return images;
    }

    private async Task<IList<ContainerListResponse>> GetKafkaContainersAsync(CancellationToken cancellationToken)
    {
        var containers = await DockerClient
            .Containers
            .ListContainersAsync(new ContainersListParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    ["ancestor"] = new Dictionary<string, bool>
                    {
                        [Image.ID] = true
                    }
                }
            }, cancellationToken).ConfigureAwait(false);

        return containers;
    }
}
