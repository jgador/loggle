# Go OpenTelemetry Logging Sample

Run with a Go toolchain 1.22+:

```bash
go run .
```

Environment variables:

- `LOGGLE_OTLP_ENDPOINT` (default `http://localhost:4318/v1/logs`)
- `LOGGLE_BEARER_TOKEN` (optional)
- `LOGGLE_SERVICE_NAME`
- `LOGGLE_SERVICE_VERSION`
- `LOGGLE_ENVIRONMENT`

The example builds the OTLP/HTTP JSON payload manually and posts it with the standard library HTTP client (no external dependencies).
