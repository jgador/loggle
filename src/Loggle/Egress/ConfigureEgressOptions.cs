using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Loggle.Egress;
internal class ConfigureEgressOptions : NamedConfigureFromConfigurationOptions<EgressOptions>
{
    public ConfigureEgressOptions(string? name, IConfiguration config)
        : base(name, config)
    {
    }
}
