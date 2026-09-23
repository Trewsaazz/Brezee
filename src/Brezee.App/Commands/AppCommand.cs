using System.Globalization;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Brezee.App.Commands;

// An application command: a stable id, display text and an optional shortcut around an ICommand.
// Menus, toolbars and key bindings all use these, so they always stay in sync.
public sealed partial class AppCommand : ObservableObject, ICommand
{
    private readonly ICommand _inner;

    public AppCommand(string id, string text, ICommand inner, KeyGesture? gesture = null)
    {
        Id = id;
        Text = text;
        _inner = inner;
        Gesture = gesture;
    }

    // Stable identifier, e.g. "view.output". Used for lookups and, later, for saved shortcut overrides.
    public string Id { get; }

    // Display text for menus. An underscore marks the access key, e.g. "E_xit".
    public string Text { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GestureText))]
    public partial KeyGesture? Gesture { get; set; }

    public string GestureText => Gesture?.GetDisplayStringForCulture(CultureInfo.CurrentUICulture) ?? string.Empty;

    public event EventHandler? CanExecuteChanged
    {
        add => _inner.CanExecuteChanged += value;
        remove => _inner.CanExecuteChanged -= value;
    }

    public bool CanExecute(object? parameter) => _inner.CanExecute(parameter);

    public void Execute(object? parameter) => _inner.Execute(parameter);
}
