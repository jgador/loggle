# Loggle

Loggle is an exporter helper for .NET applications that routes structured logs through the OpenTelemetry collector bundled with the Loggle stack. This package contains the `AddLoggleExporter()` extensions plus option types that make it trivial to push local logs into Elasticsearch/Kibana.

## Install

```powershell
dotnet add package Loggle --version 1.0.0-rc1
```

## Configure logging

Add Loggle-specific settings to your configuration (for example `appsettings.json`):

```json
{
  "Logging": {
    "OpenTelemetry": {
      "IncludeFormattedMessage": true,
      "IncludeScopes": true,
      "ParseStateValues": true
    },
    "Loggle": {
      "ServiceName": "Examples.Loggle.Console",
      "ServiceVersion": "1.0.0-rc1",
      "OtelCollector": {
        "BearerToken": "REPLACE_WITH_YOUR_OWN_SECRET",
        "LogsReceiverEndpoint": "http://your-domain-or-ip:4318/v1/logs"
      }
    }
  }
}
```

Then register the exporter during startup:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Loggle;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddLoggleExporter();
    });

var host = builder.Build();
await host.RunAsync();
```

## Targets

`Loggle` multi-targets `net10.0`, `net9.0`, `net8.0`, and `netstandard2.0`, so it works in modern ASP.NET Core, worker services, and legacy libraries.

## More information

- Source code and local/Cloud deployment docs live at [https://github.com/jgador/loggle](https://github.com/jgador/loggle)
- Issues and feature requests are welcome in the GitHub repo.

Happy logging!
