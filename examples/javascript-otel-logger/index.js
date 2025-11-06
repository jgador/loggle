import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

import { diag, DiagConsoleLogger, DiagLogLevel } from "@opentelemetry/api";
import { Resource } from "@opentelemetry/resources";
import {
  LoggerProvider,
  BatchLogRecordProcessor,
} from "@opentelemetry/sdk-logs";
import { OTLPLogExporter } from "@opentelemetry/exporter-logs-otlp-http";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

function loadEnvFile() {
  const envPath = path.join(__dirname, ".env");
  if (!fs.existsSync(envPath)) {
    return;
  }

  const content = fs.readFileSync(envPath, "utf8");
  for (const rawLine of content.split(/\r?\n/)) {
    const line = rawLine.trim();
    if (!line || line.startsWith("#") || !line.includes("=")) {
      continue;
    }

    const [keyPart, ...rest] = line.split("=");
    const key = keyPart.trim();
    const value = rest.join("=").trim().replace(/^['"]|['"]$/g, "");
    if (key && !(key in process.env)) {
      process.env[key] = value;
    }
  }
}

class Config {
  constructor({ endpoint, bearerToken, serviceName, serviceVersion, environment }) {
    this.endpoint = endpoint;
    this.bearerToken = bearerToken;
    this.serviceName = serviceName;
    this.serviceVersion = serviceVersion;
    this.environment = environment;
  }

  static fromEnv() {
    const requiredKeys = [
      "LOGGLE_OTLP_ENDPOINT",
      "LOGGLE_SERVICE_NAME",
      "LOGGLE_SERVICE_VERSION",
      "LOGGLE_ENVIRONMENT",
    ];

    const values = {};
    const missing = [];
    for (const key of requiredKeys) {
      const value = process.env[key];
      if (!value) {
        missing.push(key);
      } else {
        values[key] = value;
      }
    }

    if (missing.length > 0) {
      throw new Error(
        `Missing required configuration value(s): ${missing.join(
          ", ",
        )}. Ensure examples/javascript-otel-logger/.env is populated.`,
      );
    }

    return new Config({
      endpoint: values.LOGGLE_OTLP_ENDPOINT,
      bearerToken: process.env.LOGGLE_BEARER_TOKEN ?? null,
      serviceName: values.LOGGLE_SERVICE_NAME,
      serviceVersion: values.LOGGLE_SERVICE_VERSION,
      environment: values.LOGGLE_ENVIRONMENT,
    });
  }
}

loadEnvFile();

const config = Config.fromEnv();

diag.setLogger(new DiagConsoleLogger(), DiagLogLevel.ERROR);

const headers = {};
if (config.bearerToken) {
  headers["Authorization"] = `Bearer ${config.bearerToken}`;
}

const resource = Resource.default().merge(
  new Resource({
    "service.name": config.serviceName,
    "service.version": config.serviceVersion,
    "deployment.environment": config.environment,
  }),
);

const provider = new LoggerProvider({ resource });
const exporter = new OTLPLogExporter({ url: config.endpoint, headers });
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
