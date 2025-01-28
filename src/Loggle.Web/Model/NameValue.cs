using Nest;

namespace Loggle.Web.Model;

public class NameValue
{
    [Keyword(
        Name = "name",
        Index = true,
        DocValues = true,
        Norms = false)]
    public string? Name { get; set; }

    [Keyword(
        Name = "value",
        Index = true,
        DocValues = true,
        Norms = false)]
    public string? Value { get; set; }
}
