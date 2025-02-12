# Loggle - Docker Setup

Follow these steps to build and run the Loggle API and OpenTelemetry Collector.

1. **Navigate to the root directory of the project:**
```sh
docker build -t loggle-web:latest -f .\docker\Dockerfile --no-cache .
```
2. **Run using Docker Compose:**
```sh
docker-compose -f .\docker\docker-compose.yml --project-name loggle up -d
```

Once running, logs will be collected and forwarded to the ASP.NET Core API with an API key in the headers.

## Data Flow
The setup sends logs to the collector, which exports them to an ASP.NET Core API:

```
+------------------+      +-------------------------+      +--------------+
| Application Logs | ---> | OpenTelemetry Collector | ---> | ASP.NET Core |
+------------------+      +-------------------------+      +--------------+
```