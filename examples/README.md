# Cross-language Logging Samples

The `examples` directory now contains minimal OpenTelemetry logging snippets for common languages. Each sample targets an OTLP/HTTP collector compatible with Loggle.

## Available languages

- `python-otel-logger`
- `javascript-otel-logger`
- `typescript-otel-logger`
- `go-otel-logger`
- `Examples.Loggle.Console` (.NET)

Every sample respects the following environment variables:

- `LOGGLE_OTLP_ENDPOINT` (required unless the default `http://localhost:4318/v1/logs` applies)
- `LOGGLE_BEARER_TOKEN` (optional)
- `LOGGLE_SERVICE_NAME`
- `LOGGLE_SERVICE_VERSION`
- `LOGGLE_ENVIRONMENT`

Each subfolder includes language-specific instructions in its README.

### .NET Example

The existing .NET sample lives in `Examples.Loggle.Console`. To run it:

```powershell
cd Examples.Loggle.Console
dotnet run
```

It uses the same configuration pattern (`appsettings.json`) to target your Loggle collector and is also callable from the shared runner (use `-Language csharp`).

## Runner script

Valid `-Language` values: `csharp`, `python`, `javascript`, `typescript`, `go`.

Use `run-examples.ps1` to execute one sample at a time from PowerShell 7+:

```powershell
cd examples
.\run-examples.ps1 -Language python -OtlpEndpoint "http://localhost:4318/v1/logs"
# Provide -BearerToken if your collector enforces authentication:
# .\run-examples.ps1 -Language python -BearerToken "your-shared-secret"
# Use -Continuous to loop the selected sample until Ctrl+C:
# .\run-examples.ps1 -Language python -Continuous
```

The script installs dependencies where practical (for example `pip install` or `npm install --legacy-peer-deps`) and restores the caller's environment variables on completion. For Python, it attempts to enable `pip` automatically via `python -m ensurepip` if it is not already available. If `-OtlpEndpoint` is not provided, the script defaults to `http://localhost:4318/v1/logs`.
If `-BearerToken` is omitted, the runner supplies the placeholder value `REPLACE_WITH_YOUR_OWN_SECRET`; update your collector configuration to match or override it when invoking the script.
