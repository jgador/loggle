# JavaScript OpenTelemetry Logging Sample

Install dependencies and run the script:

```bash
npm install
LOGGLE_OTLP_ENDPOINT="http://localhost:4318/v1/logs" npm start
```

Optional environment variables:

- `LOGGLE_BEARER_TOKEN`
- `LOGGLE_SERVICE_NAME`
- `LOGGLE_SERVICE_VERSION`
- `LOGGLE_ENVIRONMENT`

The sample uses the OpenTelemetry JS SDK to send log records over OTLP/HTTP.
