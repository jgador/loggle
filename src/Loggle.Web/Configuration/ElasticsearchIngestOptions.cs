namespace Loggle.Web.Configuration;

public class ElasticsearchIngestOptions
{
    public const string SectionKey = "Logging:Loggle";

    public string? ElasticsearchIngestUrl { get; set; }

    public string DataStreamName { get; set; } = string.Empty;
}
