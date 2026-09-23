using System.Windows;
using Brezee.App.Commands;
using Brezee.App.Features.Explorer;
using Brezee.App.Features.Output;
using Brezee.App.Shell;
using Microsoft.Extensions.DependencyInjection;

namespace Brezee.App;

public partial class App : Application
{
    private ServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _services = ConfigureServices();

        var window = _services.GetRequiredService<MainWindow>();
        MainWindow = window;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        base.OnExit(e);
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Shell
        services.AddSingleton<CommandRegistry>();
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
}
