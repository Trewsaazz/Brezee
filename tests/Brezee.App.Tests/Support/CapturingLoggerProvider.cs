using Microsoft.Extensions.Logging;

namespace Brezee.App.Tests.Support;

public sealed record LogEntry(string Category, LogLevel Level, string Message);

// Records every log message so tests can assert on what was logged.
public sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly List<LogEntry> _entries = [];

    public IReadOnlyList<LogEntry> Entries
    {
        get { lock (_entries) return _entries.ToList(); }
    }

    public ILoggerFactory CreateFactory() => LoggerFactory.Create(builder =>
    {
        builder.SetMinimumLevel(LogLevel.Trace);
        builder.AddProvider(this);
    });

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(this, categoryName);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(CapturingLoggerProvider provider, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            lock (provider._entries)
                provider._entries.Add(new LogEntry(category, logLevel, formatter(state, exception)));
        }
    }
}
