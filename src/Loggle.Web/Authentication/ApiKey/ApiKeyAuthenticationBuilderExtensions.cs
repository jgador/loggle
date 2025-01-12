using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Loggle.Web.Authentication.ApiKey;

public static class ApiKeyAuthenticationBuilderExtensions
{
    public static AuthenticationBuilder AddApiKey([NotNull] this IServiceCollection services, [AllowNull] Action<ApiKeyOptions> configureOptions = null)
    {
        services.TryAddTransient<IApiKeyValidator, AlwaysApprovedApiKeyValidator>();
        services.TryAddTransient<ApiKeyAuthenticationHandler>();

        services.Configure<AuthenticationOptions>(options =>
        {
            options.AddScheme(ApiKeyOptions.AuthenticationScheme, builder =>
            {
                builder.HandlerType = typeof(ApiKeyAuthenticationHandler);
                builder.DisplayName = null;
            });
        });

        return services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = ApiKeyOptions.AuthenticationScheme;
            options.DefaultChallengeScheme = ApiKeyOptions.AuthenticationScheme;
        });
    }
}
