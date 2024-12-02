using Microsoft.Extensions.Logging;

namespace Loggle.Logging;

internal sealed class LoggleLogger : ILogger
{
    private readonly string _name;

    internal LoggleLogger(
        string name,
        IExternalScopeProvider? scopeProvider,
        LoggleLoggerOptions options)
    {
        ThrowHelper.ThrowIfNull(name);

        _name = name;
        ScopeProvider = scopeProvider;
        Options = options;
    }

    internal IExternalScopeProvider? ScopeProvider { get; set; }

    internal LoggleLoggerOptions Options { get; set; }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => ScopeProvider?.Push(state);

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        ThrowHelper.ThrowIfNull(formatter);

        var message = formatter(state, exception);

        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        message = $"{logLevel}: {message}";

        if (exception != null)
        {
            message += Environment.NewLine + Environment.NewLine + exception;
        }

        // TODO: Push to kafka via the producer
    }
}
