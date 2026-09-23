using Brezee.App.Diagnostics;
using Brezee.App.Tests.Support;
using Brezee.Bridge;
using Microsoft.Extensions.Logging;

namespace Brezee.App.Tests.Diagnostics;

// Runs the real native core through the C++/CLI bridge. The core's log sink is global, so these
// tests must not run in parallel with each other.
[Collection(nameof(CoreRuntimeTests))]
public sealed class CoreRuntimeTests : IDisposable
{
    public void Dispose() => CoreRuntime.Shutdown();

    [Fact]
    public void Initialize_DeliversCoreLogMessagesToTheHandler()
    {
        var received = new List<(CoreLogLevel Level, string Category, string Message)>();

        CoreRuntime.Initialize((level, category, message) => received.Add((level, category, message)));

        Assert.Contains(received, m =>
            m.Level == CoreLogLevel.Info && m.Category == "Runtime" && m.Message.Contains("Firebird client"));
        Assert.Contains(received, m =>
            m.Level == CoreLogLevel.Info && m.Category == "Runtime" && m.Message.Contains("initialized"));
    }

    [Fact]
    public void Initialize_WithForwarder_LogsUnderCoreCategory()
    {
        using var capture = new CapturingLoggerProvider();
        using var factory = capture.CreateFactory();
        var forwarder = new CoreLogForwarder(factory);

        CoreRuntime.Initialize(forwarder.Log);

        Assert.NotEmpty(capture.Entries);
        Assert.All(capture.Entries, entry =>
        {
            Assert.Equal("Core.Runtime", entry.Category);
            Assert.Equal(LogLevel.Information, entry.Level);
        });
    }

    [Fact]
    public void Initialize_WithoutHandler_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => CoreRuntime.Initialize(null!));
    }

    [Fact]
    public void Shutdown_DetachesTheHandler()
    {
        var first = 0;
        CoreRuntime.Initialize((_, _, _) => first++);
        CoreRuntime.Shutdown();
        var firstAfterShutdown = first;

        var second = 0;
        CoreRuntime.Initialize((_, _, _) => second++);

        Assert.True(firstAfterShutdown > 0);
        Assert.Equal(firstAfterShutdown, first); // The first handler heard nothing after Shutdown.
        Assert.True(second > 0);
    }

    [Fact]
    public void Initialize_HandlerThatThrows_DoesNotBreakTheCore()
    {
        var exception = Record.Exception(() =>
            CoreRuntime.Initialize((_, _, _) => throw new InvalidOperationException("handler failed")));

        Assert.Null(exception);
    }
}
