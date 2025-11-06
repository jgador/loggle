# Cross-language Logging Samples

## .NET (dotnet-otel-logger)
- Run `dotnet run` inside `examples/dotnet-otel-logger`.
- Adjust `appsettings.json` (`Logging:Loggle:OtelCollector`) for endpoint, token, and service metadata.
- Works against the same OTLP collector as the other samples.

## Go (go-otel-logger)
- Run with `go run .` from `examples/go-otel-logger`.
- Update `config.json` (`loggle.*`) for endpoint, token, and service metadata.
- Emits a handful of sample logs, finishing with a WARN entry.

## Python (python-otel-logger)
- Install deps via `python -m pip install -r requirements.txt`.
- Provide settings in `.env`; they hydrate the `Config` dataclass on startup.
- Produces structured INFO/WARN entries plus an expected exception example.

## JavaScript (javascript-otel-logger)
- Install with `npm install`, then run `npm start`.
- Configuration is read from `.env`; `index.js` validates required keys.
- Cycles through INFO, WARN, and ERROR severities for six example logs.

## TypeScript (typescript-otel-logger)
- Install with `npm install`; execute using `npm start` (ts-node).
- Reads `.env` into a typed `Config` class mirroring the JavaScript pattern.
- Emits five INFO logs and a final WARN before shutdown.

## Runner script
- `examples/run-examples.ps1 -Language <lang>` loops any sample until Ctrl+C.
- Each language folder keeps its own configuration (`config.json`, `.env`, or similar); adjust those files before running.
- The runner restores per-language dependencies (pip/npm/dotnet) so you can focus on configs.

## Shared bearer token
- The collector (`examples/otel-collector-config.yaml`) expects the token `REPLACE_WITH_YOUR_OWN_SECRET`.
- Ensure every example uses the same value in its config file (`.env`, `config.json`, or `appsettings.json`) before sending logs.
