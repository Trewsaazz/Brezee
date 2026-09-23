using System.Windows;
using Brezee.App.Commands;
using Brezee.App.Connections;
using Brezee.App.Diagnostics;
using Brezee.App.Features.Explorer;
using Brezee.App.Features.Output;
using Brezee.App.Shell;
using Brezee.Bridge;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Brezee.App;

public partial class App : Application
{
    private ServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _services = ConfigureServices();

        var errorHandler = _services.GetRequiredService<ErrorHandler>();
        errorHandler.Install(this);

        var logger = _services.GetRequiredService<ILogger<App>>();
        logger.LogInformation("Brezee starting, logging to {LogDirectory}", _services.GetRequiredService<LogFiles>().Directory);

        try
        {
            CoreRuntime.Initialize(_services.GetRequiredService<CoreLogForwarder>().Log);
        }
        catch (CoreException ex)
        {
            errorHandler.Report(ex);
            Shutdown(1);
            return;
        }

        var window = _services.GetRequiredService<MainWindow>();
        MainWindow = window;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Disconnect while the core can still log, then detach the log sink.
        _services?.GetService<ConnectionManager>()?.Dispose();
        CoreRuntime.Shutdown();
        _services?.GetService<ILogger<App>>()?.LogInformation("Brezee exiting with code {ExitCode}", e.ApplicationExitCode);
        _services?.Dispose(); // Flushes the log files.
        base.OnExit(e);
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Diagnostics
        var logFiles = new LogFiles();
        services.AddSingleton(logFiles);
        services.AddLogging(logging =>
        {
#if DEBUG
            logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
#else
            logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
#endif
            logging.AddSerilog(CreateFileLogger(logFiles), dispose: true);
        });
        services.AddSingleton<ILoggerProvider, OutputPanelLoggerProvider>();
        services.AddSingleton<CoreLogForwarder>();
        services.AddSingleton<ErrorHandler>();

        // Connections
        services.AddSingleton<IDatabaseConnector, FirebirdConnector>();
        services.AddSingleton<ConnectionManager>();

        // Shell
        services.AddSingleton<CommandRegistry>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<MainWindow>();

        // Tool panels
        services.AddSingleton<ExplorerViewModel>();
        services.AddSingleton<OutputViewModel>();

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
    }

    private static Serilog.ILogger CreateFileLogger(LogFiles logFiles) =>
        new LoggerConfiguration()
            .MinimumLevel.Verbose() // Filtering is done by Microsoft.Extensions.Logging above.
            .WriteTo.File(
                logFiles.FilePattern,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: LogFiles.RetainedDays,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
}
