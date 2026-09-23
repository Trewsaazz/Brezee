using System.Windows.Threading;
using Brezee.App.Features.Output;
using Microsoft.Extensions.Logging;

namespace Brezee.App.Diagnostics;

// Shows log messages at Information level and above in the Output panel.
public sealed class OutputPanelLoggerProvider : ILoggerProvider
{
    private readonly OutputViewModel _output;
    private readonly Dispatcher _dispatcher;

    public OutputPanelLoggerProvider(OutputViewModel output)
    {
        _output = output;
        // Created on the UI thread at startup; log calls from other threads are marshalled back to it.
        _dispatcher = Dispatcher.CurrentDispatcher;
    }

    public ILogger CreateLogger(string categoryName) => new OutputPanelLogger(this);

    public void Dispose()
    {
    }

    private void Write(LogLevel level, string message)
    {
        var line = level switch
        {
            LogLevel.Warning => $"Warning: {message}",
            LogLevel.Error or LogLevel.Critical => $"Error: {message}",
            _ => message,
        };

        if (_dispatcher.CheckAccess())
            _output.WriteLine(line);
        else
            _dispatcher.BeginInvoke(() => _output.WriteLine(line));
    }

    private sealed class OutputPanelLogger(OutputPanelLoggerProvider provider) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information && logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);
            if (exception is not null)
                message = $"{message} ({exception.Message})";

            provider.Write(logLevel, message);
        }
    }
}
