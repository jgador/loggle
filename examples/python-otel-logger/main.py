import logging
import os
import sys
import time
from dataclasses import dataclass
from pathlib import Path
from typing import Optional

try:
    from opentelemetry import logs  # type: ignore[attr-defined]
except ImportError:  # pragma: no cover
    from opentelemetry import _logs as logs  # type: ignore[attr-defined]
try:
    from opentelemetry.sdk.logs import LoggerProvider, LoggingHandler
    from opentelemetry.sdk.logs.export import BatchLogRecordProcessor
except ImportError:  # pragma: no cover
    from opentelemetry.sdk._logs import LoggerProvider, LoggingHandler  # type: ignore[attr-defined]
    from opentelemetry.sdk._logs.export import BatchLogRecordProcessor  # type: ignore[attr-defined]
from opentelemetry.exporter.otlp.proto.http._log_exporter import OTLPLogExporter
from opentelemetry.sdk.resources import Resource


def load_env_file():
    env_path = Path(__file__).resolve().parent / ".env"
    if not env_path.exists():
        return

    for raw_line in env_path.read_text().splitlines():
        line = raw_line.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue
        key, value = line.split("=", 1)
        key = key.strip()
        value = value.strip().strip('"').strip("'")
        if key and key not in os.environ:
            os.environ[key] = value


@dataclass
class Config:
    endpoint: str
    bearer_token: Optional[str]
    service_name: str
    service_version: str
    environment: str


def load_config() -> Config:
    required_keys = {
        "LOGGLE_OTLP_ENDPOINT": "endpoint",
        "LOGGLE_SERVICE_NAME": "service_name",
        "LOGGLE_SERVICE_VERSION": "service_version",
        "LOGGLE_ENVIRONMENT": "environment",
    }

    values = {}
    missing = []
    for key, field_name in required_keys.items():
        value = os.environ.get(key)
        if not value:
            missing.append(key)
        else:
            values[field_name] = value

    if missing:
        formatted = ", ".join(missing)
        raise RuntimeError(f"Missing required configuration value(s): {formatted}. Ensure examples/python-otel-logger/.env is populated.")

    bearer = os.environ.get("LOGGLE_BEARER_TOKEN")
    return Config(
        endpoint=values["endpoint"],
        bearer_token=bearer if bearer else None,
        service_name=values["service_name"],
        service_version=values["service_version"],
        environment=values["environment"],
    )


def configure_logger(config: Config):
    headers = {}
    if config.bearer_token:
        headers["Authorization"] = f"Bearer {config.bearer_token}"

    resource = Resource.create(
        {
            "service.name": config.service_name,
            "service.version": config.service_version,
            "deployment.environment": config.environment,
        }
    )

    provider = LoggerProvider(resource=resource)
    exporter = OTLPLogExporter(endpoint=config.endpoint, headers=headers)
    processor = BatchLogRecordProcessor(exporter)
    provider.add_log_record_processor(processor)
    logs.set_logger_provider(provider)

    handler = LoggingHandler(level=logging.INFO, logger_provider=provider)
    logger = logging.getLogger("loggle.python.example")
    logger.setLevel(logging.INFO)
    logger.addHandler(handler)

    return logger, processor


def run(logger):
    for idx in range(5):
        logger.info("python log message %s", idx, extra={"iteration": idx})
        time.sleep(0.25)
    logger.warning("python warning with structured data", extra={"user.id": 42, "feature": "demo"})
    try:
        1 / 0
    except ZeroDivisionError:
        logger.exception("Expected failure when dividing by zero")


def main():
    load_env_file()
    config = load_config()
    logger, processor = configure_logger(config)
    try:
        run(logger)
    finally:
        processor.shutdown()


if __name__ == "__main__":
    sys.exit(main())
