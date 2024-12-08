using Loggle.Egress;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Loggle.Logging;

public static class LoggleLoggerExtensions
{
    public static ILoggingBuilder AddLoggle(this ILoggingBuilder builder)
    {
        builder.Services.AddSingleton<ILoggerProvider, LoggleLoggerProvider>();
        builder.Services.TryAddTransient<IConfigureOptions<EgressOptions>, ConfigureEgressOptions>();

        return builder;
    }
}
