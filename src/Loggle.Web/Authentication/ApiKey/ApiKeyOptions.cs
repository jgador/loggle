using Microsoft.AspNetCore.Authentication;

namespace Loggle.Web.Authentication.ApiKey;

public class ApiKeyOptions : AuthenticationSchemeOptions
{
    public const string AuthenticationScheme = "ApiKey";

    public string LoggleKeyHeader { get; set; } = "X-Loggle-Key";
}
