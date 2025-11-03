# Python OpenTelemetry Logging Sample

Run the sample after installing dependencies:

```bash
python -m pip install -r requirements.txt
LOGGLE_OTLP_ENDPOINT="http://localhost:4318/v1/logs" \
  python main.py
```

Set `LOGGLE_BEARER_TOKEN` if your collector expects an authorization header. Additional optional variables:

- `LOGGLE_SERVICE_NAME`
- `LOGGLE_SERVICE_VERSION`
- `LOGGLE_ENVIRONMENT`

The script sends structured log records to the configured OTLP/HTTP endpoint using the standard OpenTelemetry SDK.
