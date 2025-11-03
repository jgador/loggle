import { diag, DiagConsoleLogger, DiagLogLevel } from "@opentelemetry/api";
import { Resource } from "@opentelemetry/resources";
import {
  LoggerProvider,
  BatchLogRecordProcessor,
} from "@opentelemetry/sdk-logs";
import { OTLPLogExporter } from "@opentelemetry/exporter-logs-otlp-http";

diag.setLogger(new DiagConsoleLogger(), DiagLogLevel.ERROR);

const endpoint =
  process.env.LOGGLE_OTLP_ENDPOINT || "http://localhost:4318/v1/logs";
const bearer = process.env.LOGGLE_BEARER_TOKEN;

const headers = {};
if (bearer) {
  headers["Authorization"] = `Bearer ${bearer}`;
}

const resource = Resource.default().merge(
  new Resource({
    "service.name": process.env.LOGGLE_SERVICE_NAME || "loggle-javascript-example",
    "service.version": process.env.LOGGLE_SERVICE_VERSION || "0.1.0",
    "deployment.environment": process.env.LOGGLE_ENVIRONMENT || "local",
  }),
);

const provider = new LoggerProvider({ resource });
const exporter = new OTLPLogExporter({ url: endpoint, headers });
provider.addLogRecordProcessor(new BatchLogRecordProcessor(exporter));

const logger = provider.getLogger("loggle.javascript.example");

const severities = [
  { text: "INFO", number: 9 },
  { text: "WARN", number: 13 },
  { text: "ERROR", number: 17 },
];

let iteration = 0;

function emit() {
  const severity = severities[iteration % severities.length];
  logger.emit({
    body: `JavaScript log ${iteration}`,
    severityNumber: severity.number,
    severityText: severity.text,
    attributes: {
      "log.iteration": iteration,
      "logger.language": "javascript",
    },
  });
  iteration += 1;
  if (iteration < 6) {
    setTimeout(emit, 200);
  } else {
    setTimeout(async () => {
      await provider.shutdown();
    }, 500);
  }
}

emit();
