using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Loggle.Logging;

[ProviderAlias("Loggle")]
public class LoggleLoggerProvider : ILoggerProvider
{
    private readonly IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();
    private readonly IOptionsMonitor<LoggleLoggerOptions> _options;
    private readonly ConcurrentDictionary<string, LoggleLogger> _loggers;

    public LoggleLoggerProvider(IOptionsMonitor<LoggleLoggerOptions> options)
    {
        _options = options;
        _loggers = new ConcurrentDictionary<string, LoggleLogger>();
    }

    public ILogger CreateLogger(string name)
    {
        ThrowHelper.ThrowIfNull(name);

        return _loggers.TryGetValue(name, out LoggleLogger? logger)
            ? logger
            : _loggers.GetOrAdd(name, new LoggleLogger(name, _scopeProvider, _options.CurrentValue));
    }

    internal IExternalScopeProvider? ScopeProvider { get; set; }

    public void Dispose()
    {
    }
}
