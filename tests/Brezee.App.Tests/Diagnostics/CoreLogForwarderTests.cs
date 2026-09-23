using Brezee.App.Diagnostics;
using Brezee.App.Tests.Support;
using Brezee.Bridge;
using Microsoft.Extensions.Logging;

namespace Brezee.App.Tests.Diagnostics;

public class CoreLogForwarderTests
{
    [Theory]
    [InlineData(CoreLogLevel.Trace, LogLevel.Trace)]
    [InlineData(CoreLogLevel.Debug, LogLevel.Debug)]
    [InlineData(CoreLogLevel.Info, LogLevel.Information)]
    [InlineData(CoreLogLevel.Warning, LogLevel.Warning)]
    [InlineData(CoreLogLevel.Error, LogLevel.Error)]
    public void ToLogLevel_MapsEveryCoreLevel(CoreLogLevel core, LogLevel expected)
    {
        Assert.Equal(expected, CoreLogForwarder.ToLogLevel(core));
    }

    [Fact]
    public void Log_WritesMessageUnderCorePrefixedCategory()
    {
        using var capture = new CapturingLoggerProvider();
        using var factory = capture.CreateFactory();

        new CoreLogForwarder(factory).Log(CoreLogLevel.Warning, "Connection", "Server is slow");

        var entry = Assert.Single(capture.Entries);
        Assert.Equal(new LogEntry("Core.Connection", LogLevel.Warning, "Server is slow"), entry);
    }
}
