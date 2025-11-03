# TypeScript OpenTelemetry Logging Sample

Install dependencies and run:

```bash
npm install
LOGGLE_OTLP_ENDPOINT="http://localhost:4318/v1/logs" npm run start
```

Optional environment variables mirror the JavaScript sample:

- `LOGGLE_BEARER_TOKEN`
- `LOGGLE_SERVICE_NAME`
- `LOGGLE_SERVICE_VERSION`
- `LOGGLE_ENVIRONMENT`

The script uses the OpenTelemetry TypeScript SDK to publish OTLP/HTTP log records.
