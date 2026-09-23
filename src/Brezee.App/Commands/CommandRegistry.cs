using System.ComponentModel;
using System.Windows.Input;

namespace Brezee.App.Commands;

// Central list of every application command. XAML binds to commands by id: {Binding Commands[view.output]}.
public sealed class CommandRegistry
{
    private readonly Dictionary<string, AppCommand> _commands = new(StringComparer.Ordinal);

    // Raised when any command's shortcut changes, so key bindings can be rebuilt.
    public event EventHandler? GesturesChanged;

    public AppCommand this[string id] =>
        _commands.TryGetValue(id, out var command)
            ? command
            : throw new KeyNotFoundException($"Unknown command '{id}'.");

    public IReadOnlyCollection<AppCommand> All => _commands.Values;

    public AppCommand Register(string id, string text, ICommand command, KeyGesture? gesture = null)
    {
        if (gesture is not null && FindByGesture(gesture) is { } existing)
            throw new InvalidOperationException($"Shortcut {gesture.DisplayString} for '{id}' is already used by '{existing.Id}'.");

        var appCommand = new AppCommand(id, text, command, gesture);
        if (!_commands.TryAdd(id, appCommand))
            throw new InvalidOperationException($"Command '{id}' is already registered.");

        appCommand.PropertyChanged += OnCommandPropertyChanged;
        return appCommand;
    }

    private AppCommand? FindByGesture(KeyGesture gesture) =>
        _commands.Values.FirstOrDefault(c =>
            c.Gesture is { } g && g.Key == gesture.Key && g.Modifiers == gesture.Modifiers);

    private void OnCommandPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppCommand.Gesture))
            GesturesChanged?.Invoke(this, EventArgs.Empty);
    }
}
