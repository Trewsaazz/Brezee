using Brezee.App.Diagnostics;
using Brezee.App.Features.Output;
using Microsoft.Extensions.Logging;

namespace Brezee.App.Tests.Diagnostics;

public class OutputPanelLoggerProviderTests
{
    private readonly OutputViewModel _output = new();
    private readonly ILogger _logger;

    public OutputPanelLoggerProviderTests()
    {
        // Created on the test thread, so writes happen synchronously.
        _logger = new OutputPanelLoggerProvider(_output).CreateLogger("Test");
    }

    [Fact]
    public void Information_AppearsInOutputPanel()
    {
        _logger.LogInformation("Connected to {Database}", "employee.fdb");

        Assert.EndsWith("Connected to employee.fdb", Assert.Single(_output.Lines));
    }

    [Fact]
    public void Debug_IsNotShown()
    {
        _logger.LogDebug("Internal detail");

        Assert.Empty(_output.Lines);
    }

    [Fact]
    public void Warning_IsLabelled()
    {
        _logger.LogWarning("Server is slow");

        Assert.Contains("Warning: Server is slow", Assert.Single(_output.Lines));
    }

    [Fact]
    public void Error_IsLabelledAndIncludesExceptionMessage()
    {
        _logger.LogError(new InvalidOperationException("disk full"), "Could not save");

        var line = Assert.Single(_output.Lines);
        Assert.Contains("Error: Could not save", line);
        Assert.Contains("disk full", line);
    }
}
