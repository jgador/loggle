import { diag, DiagConsoleLogger, DiagLogLevel } from "@opentelemetry/api";
import { Resource } from "@opentelemetry/resources";
import {
  LoggerProvider,
  BatchLogRecordProcessor,
} from "@opentelemetry/sdk-logs";
import { OTLPLogExporter } from "@opentelemetry/exporter-logs-otlp-http";

diag.setLogger(new DiagConsoleLogger(), DiagLogLevel.ERROR);

const endpoint =
  process.env.LOGGLE_OTLP_ENDPOINT ?? "http://localhost:4318/v1/logs";
const bearer = process.env.LOGGLE_BEARER_TOKEN;

const headers: Record<string, string> = {};
if (bearer) {
  headers.Authorization = `Bearer ${bearer}`;
}

const resource = Resource.default().merge(
  new Resource({
    "service.name":
      process.env.LOGGLE_SERVICE_NAME ?? "loggle-typescript-example",
    "service.version": process.env.LOGGLE_SERVICE_VERSION ?? "0.1.0",
    "deployment.environment": process.env.LOGGLE_ENVIRONMENT ?? "local",
  }),
);

const provider = new LoggerProvider({ resource });
const exporter = new OTLPLogExporter({ url: endpoint, headers });
provider.addLogRecordProcessor(new BatchLogRecordProcessor(exporter));

const logger = provider.getLogger("loggle.typescript.example");

async function main(): Promise<void> {
  for (let i = 0; i < 5; i += 1) {
    logger.emit({
      body: `TypeScript log ${i}`,
      severityNumber: 9,
      severityText: "INFO",
      attributes: {
        "log.iteration": i,
        "logger.language": "typescript",
      },
    });
    await new Promise((resolve) => setTimeout(resolve, 200));
  }

  logger.emit({
    body: "TypeScript example completed",
    severityNumber: 13,
    severityText: "WARN",
    attributes: {
      "logger.language": "typescript",
      "app.status": "complete",
    },
  });

  await provider.shutdown();
}

main().catch(async (err) => {
  console.error(err);
  await provider.shutdown();
  process.exit(1);
});
