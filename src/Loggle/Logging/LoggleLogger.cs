using System;
using Microsoft.Extensions.Logging;

namespace Loggle.Logging
{
    internal sealed class LoggleLogger : ILogger
    {
        private readonly string _name;
        private readonly BufferedChannel<LogMessageEntry> _channel;

        internal LoggleLogger(
            string name,
            IExternalScopeProvider? scopeProvider,
            BufferedChannel<LogMessageEntry> channel,
            LoggleLoggerOptions options)
        {
            ThrowHelper.ThrowIfNull(name);
            ThrowHelper.ThrowIfNull(channel);

            _name = name;
            _channel = channel;
            ScopeProvider = scopeProvider;
            Options = options;
        }

        internal IExternalScopeProvider? ScopeProvider { get; set; }

        internal LoggleLoggerOptions Options { get; set; }

        public bool IsEnabled(LogLevel logLevel)
        {
            return Options switch
            {
                { Enabled: false } => false,
                { MinimumLevel: var configuredLevel } when configuredLevel >= logLevel => true,
                _ => logLevel != LogLevel.None
            };
        }

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

            if (exception != null)
            {
                message += Environment.NewLine + Environment.NewLine + exception;
            }

            var logMessageEntry = new LogMessageEntry()
            {
                Timestamp = DateTimeOffset.UtcNow,
                Level = logLevel.ToString(),
                Message = message
            };

            _channel.Writer.TryWrite(logMessageEntry);
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => ScopeProvider?.Push(state) ?? NullScope.Instance;
    }
}
