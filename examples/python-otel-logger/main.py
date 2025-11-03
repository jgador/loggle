import logging
import os
import sys
import time

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
from opentelemetry.sdk.resources import Resource
from opentelemetry.exporter.otlp.proto.http._log_exporter import OTLPLogExporter


def configure_logger():
    endpoint = os.environ.get("LOGGLE_OTLP_ENDPOINT", "http://localhost:4318/v1/logs")
    bearer = os.environ.get("LOGGLE_BEARER_TOKEN")

    headers = {}
    if bearer:
        headers["Authorization"] = f"Bearer {bearer}"

    resource = Resource.create(
        {
            "service.name": os.environ.get("LOGGLE_SERVICE_NAME", "loggle-python-example"),
            "service.version": os.environ.get("LOGGLE_SERVICE_VERSION", "0.1.0"),
            "deployment.environment": os.environ.get("LOGGLE_ENVIRONMENT", "local"),
        }
    )

    provider = LoggerProvider(resource=resource)
    exporter = OTLPLogExporter(endpoint=endpoint, headers=headers)
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
    logger, processor = configure_logger()
    try:
        run(logger)
    finally:
        processor.shutdown()


if __name__ == "__main__":
    sys.exit(main())
