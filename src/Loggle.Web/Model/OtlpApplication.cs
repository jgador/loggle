using System.Collections.Generic;
using System.Linq;
using OpenTelemetry.Proto.Resource.V1;

namespace Loggle.Web.Model;

public class OtlpApplication
{
    public const string ServiceName = "service.name";
    public const string ServiceVersion = "service.version";
    public const string ServiceInstanceId = "service.instance.id";

    public string ApplicationName =>
        _serviceProperties.FirstOrDefault(p => p.Key == ServiceName).Value ?? string.Empty;

    public string InstanceId =>
        _serviceProperties.FirstOrDefault(p => p.Key == ServiceInstanceId).Value ?? string.Empty;

    public string VersionNumber =>
        _serviceProperties.FirstOrDefault(p => p.Key == ServiceVersion).Value ?? string.Empty;

    private readonly KeyValuePair<string, string>[] _serviceProperties;

    public OtlpApplication(OtlpContext context, Resource resource)
    {
        var serviceProperties = resource.Attributes.ToKeyValuePairs(context, filter: attribute =>
        {
            switch (attribute.Key)
            {
                case ServiceName:
                case ServiceVersion:
                case ServiceInstanceId:
                    return true;

                default:
                    return false;
            }
        });

        _serviceProperties = serviceProperties;
    }
}
