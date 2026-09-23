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

        var (level, category, message) = Assert.Single(received);
        Assert.Equal(CoreLogLevel.Info, level);
        Assert.Equal("Runtime", category);
        Assert.Contains("initialized", message);
    }

    [Fact]
    public void Initialize_WithForwarder_LogsUnderCoreCategory()
    {
        using var capture = new CapturingLoggerProvider();
        using var factory = capture.CreateFactory();
        var forwarder = new CoreLogForwarder(factory);

        CoreRuntime.Initialize(forwarder.Log);

        var entry = Assert.Single(capture.Entries);
        Assert.Equal("Core.Runtime", entry.Category);
        Assert.Equal(LogLevel.Information, entry.Level);
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

        var second = 0;
        CoreRuntime.Initialize((_, _, _) => second++);

        Assert.Equal(1, first);  // Nothing more after Shutdown.
        Assert.Equal(1, second);
    }

    [Fact]
    public void Initialize_HandlerThatThrows_DoesNotBreakTheCore()
    {
        var exception = Record.Exception(() =>
            CoreRuntime.Initialize((_, _, _) => throw new InvalidOperationException("handler failed")));

        Assert.Null(exception);
    }
}
