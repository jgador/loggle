using System.Collections.Generic;
using System.Linq;
using OpenTelemetry.Proto.Resource.V1;

namespace Loggle.Web.Model;

public class OtlpApplication
{
    public const string ServiceNameKey = "service.name";
    public const string ServiceVersionKey = "service.version";
    public const string ServiceInstanceIdKey = "service.instance.id";

    public string ServiceName =>
        _serviceProperties.FirstOrDefault(p => p.Key == ServiceNameKey).Value ?? string.Empty;

    public string ServiceInstanceId =>
        _serviceProperties.FirstOrDefault(p => p.Key == ServiceInstanceIdKey).Value ?? string.Empty;

    public string ServiceVersionNumber =>
        _serviceProperties.FirstOrDefault(p => p.Key == ServiceVersionKey).Value ?? string.Empty;

    private readonly KeyValuePair<string, string>[] _serviceProperties;

    public OtlpApplication(OtlpContext context, Resource resource)
    {
        var serviceProperties = resource.Attributes.ToKeyValuePairs(context, filter: attribute =>
        {
            switch (attribute.Key)
            {
                case ServiceNameKey:
                case ServiceVersionKey:
                case ServiceInstanceIdKey:
                    return true;

                default:
                    return false;
            }
        });

        _serviceProperties = serviceProperties ?? [];
    }
}
