using Brezee.App.Diagnostics;
using Brezee.Bridge;
using Microsoft.Extensions.Logging.Abstractions;

namespace Brezee.App.Tests.Diagnostics;

public class ErrorHandlerTests
{
    private readonly LogFiles _logFiles = new(@"C:\Users\someone\AppData\Local\Brezee\logs");
    private readonly ErrorHandler _handler;

    public ErrorHandlerTests()
    {
        _handler = new ErrorHandler(NullLogger<ErrorHandler>.Instance, _logFiles);
    }

    [Fact]
    public void DescribeForUser_CoreException_ShowsCoreMessageAsIs()
    {
        var error = new CoreException("Could not connect to localhost:3050", CoreErrorKind.Connection);

        Assert.Equal("Could not connect to localhost:3050", _handler.DescribeForUser(error));
    }

    [Fact]
    public void DescribeForUser_UnexpectedException_IncludesMessageAndLogLocation()
    {
        var description = _handler.DescribeForUser(new InvalidOperationException("Boom"));

        Assert.Contains("Boom", description);
        Assert.Contains(_logFiles.Directory, description);
    }
}
