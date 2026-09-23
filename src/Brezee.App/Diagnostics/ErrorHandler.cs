using System.Globalization;
using System.Windows;
using System.Windows.Threading;
using Brezee.App.Resources;
using Brezee.Bridge;
using Microsoft.Extensions.Logging;

namespace Brezee.App.Diagnostics;

// Single place where unexpected errors end up: they are logged and shown to the user.
public sealed class ErrorHandler(ILogger<ErrorHandler> logger, LogFiles logFiles)
{
    // Catches every exception that would otherwise crash the app or vanish silently.
    public void Install(Application application)
    {
        application.DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    // Logs an error and tells the user about it. Use for failures the app can recover from.
    public void Report(Exception exception)
    {
        logger.LogError(exception, "Unhandled error");
        ShowError(exception);
    }

    // CoreExceptions carry messages written for users; anything else is a bug, so point at the log.
    public string DescribeForUser(Exception exception) => exception switch
    {
        CoreException core => core.Message,
        _ => string.Format(CultureInfo.CurrentCulture, Strings.Error_Unexpected, exception.Message, logFiles.Directory),
    };

    private void ShowError(Exception exception)
    {
        var owner = Application.Current?.MainWindow;
        var message = DescribeForUser(exception);

        if (owner is { IsLoaded: true })
            MessageBox.Show(owner, message, Strings.Error_Title, MessageBoxButton.OK, MessageBoxImage.Error);
        else
            MessageBox.Show(message, Strings.Error_Title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // UI-thread errors are recoverable: report them and keep the app running.
        e.Handled = true;
        Report(e.Exception);
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        // A background thread crashed; the process is going down. Record why.
        if (e.ExceptionObject is Exception exception)
            logger.LogCritical(exception, "Fatal error, Brezee is closing");
        else
            logger.LogCritical("Fatal error, Brezee is closing: {Error}", e.ExceptionObject);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        logger.LogError(e.Exception, "Unobserved background task error");
        e.SetObserved();
    }
}
