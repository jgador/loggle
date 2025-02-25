using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter.Loggle;

namespace Loggle.Web.Authentication.ApiKey;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyOptions>
{
    private readonly IApiKeyValidator _apiKeyValidator;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiKeyValidator apiKeyValidator)
        : base(options, logger, encoder)
    {
        ThrowHelper.ThrowIfNull(apiKeyValidator);

        _apiKeyValidator = apiKeyValidator;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var allowAnonymous = Context
            ?.GetEndpoint()
            ?.Metadata
            ?.GetMetadata<IAllowAnonymous>() is not null;

        if (allowAnonymous)
        {
            return AuthenticateResult.NoResult();
        }

        if (!Request.Headers.TryGetValue(Options.LoggleKeyHeader, out var apiKey))
        {
            return AuthenticateResult.Fail("API Key was not provided.");
        }

        if (!await _apiKeyValidator.IsValidAsync(apiKey!))
        {
            return AuthenticateResult.Fail("Invalid API Key.");
        }

        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, ApiKeyOptions.AuthenticationScheme);

        return AuthenticateResult.Success(ticket);
    }
}
