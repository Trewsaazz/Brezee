using Brezee.Bridge;
using Microsoft.Extensions.Logging;

namespace Brezee.App.Diagnostics;

// Forwards native core log messages into the app's logging, under categories named "Core.<category>".
public sealed class CoreLogForwarder(ILoggerFactory loggerFactory)
{
    public void Log(CoreLogLevel level, string category, string message)
    {
        var logger = loggerFactory.CreateLogger($"Core.{category}");
        logger.Log(ToLogLevel(level), "{Message}", message);
    }

    public static LogLevel ToLogLevel(CoreLogLevel level) => level switch
    {
        CoreLogLevel.Trace => LogLevel.Trace,
        CoreLogLevel.Debug => LogLevel.Debug,
        CoreLogLevel.Info => LogLevel.Information,
        CoreLogLevel.Warning => LogLevel.Warning,
        CoreLogLevel.Error => LogLevel.Error,
        _ => LogLevel.Error,
    };
}
