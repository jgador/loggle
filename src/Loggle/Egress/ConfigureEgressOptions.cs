using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Loggle.Egress;

internal sealed class ConfigureEgressOptions : IConfigureOptions<EgressOptions>
{
    private readonly IConfiguration _configuration;

    public ConfigureEgressOptions(IConfiguration configuration)
    {
        ThrowHelper.ThrowIfNull(configuration);

        _configuration = configuration;
    }

    public void Configure(EgressOptions options)
    {
        _configuration.Bind(EgressOptions.SectionKey, options);
    }
}
