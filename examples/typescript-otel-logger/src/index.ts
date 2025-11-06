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

function loadEnvFile(): void {
  const envPath = path.join(__dirname, "..", ".env");
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

type RequiredConfigKey =
  | "LOGGLE_OTLP_ENDPOINT"
  | "LOGGLE_SERVICE_NAME"
  | "LOGGLE_SERVICE_VERSION"
  | "LOGGLE_ENVIRONMENT";

class Config {
  readonly endpoint: string;
  readonly bearerToken: string | null;
  readonly serviceName: string;
  readonly serviceVersion: string;
  readonly environment: string;

  private constructor(values: {
    endpoint: string;
    bearerToken: string | null;
    serviceName: string;
    serviceVersion: string;
    environment: string;
  }) {
    this.endpoint = values.endpoint;
    this.bearerToken = values.bearerToken;
    this.serviceName = values.serviceName;
    this.serviceVersion = values.serviceVersion;
    this.environment = values.environment;
  }

  static fromEnv(): Config {
    const requiredKeys: RequiredConfigKey[] = [
      "LOGGLE_OTLP_ENDPOINT",
      "LOGGLE_SERVICE_NAME",
      "LOGGLE_SERVICE_VERSION",
      "LOGGLE_ENVIRONMENT",
    ];

    const values: Partial<Record<RequiredConfigKey, string>> = {};
    const missing: RequiredConfigKey[] = [];

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
        )}. Ensure examples/typescript-otel-logger/.env is populated.`,
      );
    }

    return new Config({
      endpoint: values.LOGGLE_OTLP_ENDPOINT!,
      bearerToken: process.env.LOGGLE_BEARER_TOKEN ?? null,
      serviceName: values.LOGGLE_SERVICE_NAME!,
      serviceVersion: values.LOGGLE_SERVICE_VERSION!,
      environment: values.LOGGLE_ENVIRONMENT!,
    });
  }
}

loadEnvFile();

const config = Config.fromEnv();

diag.setLogger(new DiagConsoleLogger(), DiagLogLevel.ERROR);

const headers: Record<string, string> = {};
if (config.bearerToken) {
  headers.Authorization = `Bearer ${config.bearerToken}`;
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
