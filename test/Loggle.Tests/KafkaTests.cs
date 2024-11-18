namespace Loggle.Tests;

public class KafkaTests: IClassFixture<KafkaFixture>
{
    public KafkaTests(KafkaFixture fixture)
    {
        
    }

    [Fact]
    public async Task DownloadKafkaImage()
    {
        Assert.True(1 == 1);
    }
}
