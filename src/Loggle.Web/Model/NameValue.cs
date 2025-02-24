using System.Text.Json.Serialization;

namespace Loggle.Web.Model;

public class NameValue
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}
