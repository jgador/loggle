using System;
using System.Collections.Generic;
using OpenTelemetry.Proto.Resource.V1;

namespace Loggle.Web.Model;

public class OtlpApplication
{
    public const string ServiceNameKey = "service.name";
    public const string ServiceVersionKey = "service.version";
    public const string ServiceInstanceIdKey = "service.instance.id";

    public string ServiceName { get; private set; }

    public string ServiceVersionNumber { get; private set; }

    public string ServiceInstanceId { get; private set; }

    public readonly List<KeyValuePair<string, string>> Properties;

    public OtlpApplication(OtlpContext context, Resource resource)
    {
        Properties = [];

        foreach (var attribute in resource.Attributes)
        {
            Action assign = attribute.Key switch
            {
                ServiceNameKey => () => ServiceName = attribute.Value.GetString(),
                ServiceVersionKey => () => ServiceVersionNumber = attribute.Value.GetString(),
                ServiceInstanceIdKey => () => ServiceInstanceId = attribute.Value.GetString(),
                _ => () => Properties.Add(new KeyValuePair<string, string>(attribute.Key, attribute.Value.GetString()))
            };

            assign.Invoke();
        }
    }
}
