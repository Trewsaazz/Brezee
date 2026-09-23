using System.Windows.Input;
using Brezee.App.Commands;
using CommunityToolkit.Mvvm.Input;

namespace Brezee.App.Tests.Commands;

public class AppCommandTests
{
    [Fact]
    public void Execute_RunsWrappedCommand()
    {
        var executed = false;
        var command = new AppCommand("test.run", "Run", new RelayCommand(() => executed = true));

        command.Execute(null);

        Assert.True(executed);
    }

    [Fact]
    public void CanExecute_FollowsWrappedCommand()
    {
        var enabled = false;
        var inner = new RelayCommand(() => { }, () => enabled);
        var command = new AppCommand("test.run", "Run", inner);
        var changed = 0;
        command.CanExecuteChanged += (_, _) => changed++;

        Assert.False(command.CanExecute(null));

        enabled = true;
        inner.NotifyCanExecuteChanged();

        Assert.True(command.CanExecute(null));
        Assert.Equal(1, changed);
    }

    [Fact]
    public void GestureText_IsEmptyWithoutShortcut()
    {
        var command = new AppCommand("test.run", "Run", new RelayCommand(() => { }));

        Assert.Equal(string.Empty, command.GestureText);
    }

    [Fact]
    public void GestureText_UpdatesWhenShortcutChanges()
    {
        var command = new AppCommand("test.run", "Run", new RelayCommand(() => { }));
        var notified = new List<string?>();
        command.PropertyChanged += (_, e) => notified.Add(e.PropertyName);

        command.Gesture = new KeyGesture(Key.F5);

        Assert.NotEmpty(command.GestureText);
        Assert.Contains(nameof(AppCommand.GestureText), notified);
    }
}
