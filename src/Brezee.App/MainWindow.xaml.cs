using System.Windows;
using System.Windows.Input;
using Brezee.App.Commands;
using Brezee.App.Shell;

namespace Brezee.App;

public partial class MainWindow : Window
{
    private readonly CommandRegistry _commands;

    public MainWindow(ShellViewModel shell, CommandRegistry commands)
    {
        InitializeComponent();
        DataContext = shell;

        _commands = commands;
        _commands.GesturesChanged += (_, _) => ApplyKeyBindings();
        ApplyKeyBindings();
    }

    // Rebuilds the window's shortcuts from the command registry.
    private void ApplyKeyBindings()
    {
        InputBindings.Clear();

        foreach (var command in _commands.All)
        {
            if (command.Gesture is { } gesture)
                InputBindings.Add(new KeyBinding(command, gesture));
        }
    }
}
