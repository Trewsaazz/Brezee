using System.Windows.Input;
using Brezee.App.Commands;
using CommunityToolkit.Mvvm.Input;

namespace Brezee.App.Tests.Commands;

public class CommandRegistryTests
{
    private readonly CommandRegistry _registry = new();

    [Fact]
    public void Register_MakesCommandAvailableById()
    {
        var registered = _registry.Register("test.run", "_Run", new RelayCommand(() => { }));

        Assert.Same(registered, _registry["test.run"]);
        Assert.Equal("test.run", registered.Id);
        Assert.Equal("_Run", registered.Text);
        Assert.Contains(registered, _registry.All);
    }

    [Fact]
    public void Indexer_UnknownId_Throws()
    {
        Assert.Throws<KeyNotFoundException>(() => _registry["does.not.exist"]);
    }

    [Fact]
    public void Register_DuplicateId_Throws()
    {
        _registry.Register("test.run", "Run", new RelayCommand(() => { }));

        Assert.Throws<InvalidOperationException>(() =>
            _registry.Register("test.run", "Run again", new RelayCommand(() => { })));
    }

    [Fact]
    public void Register_DuplicateShortcut_Throws()
    {
        _registry.Register("test.first", "First", new RelayCommand(() => { }),
            new KeyGesture(Key.F5, ModifierKeys.Control));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _registry.Register("test.second", "Second", new RelayCommand(() => { }),
                new KeyGesture(Key.F5, ModifierKeys.Control)));

        Assert.Contains("test.first", ex.Message);
    }

    [Fact]
    public void Register_SameKeyWithDifferentModifiers_IsAllowed()
    {
        _registry.Register("test.first", "First", new RelayCommand(() => { }),
            new KeyGesture(Key.F5, ModifierKeys.Control));

        _registry.Register("test.second", "Second", new RelayCommand(() => { }),
            new KeyGesture(Key.F5, ModifierKeys.Control | ModifierKeys.Shift));

        Assert.Equal(2, _registry.All.Count);
    }

    [Fact]
    public void ChangingAShortcut_RaisesGesturesChanged()
    {
        var command = _registry.Register("test.run", "Run", new RelayCommand(() => { }));
        var raised = 0;
        _registry.GesturesChanged += (_, _) => raised++;

        command.Gesture = new KeyGesture(Key.F6);

        Assert.Equal(1, raised);
    }
}
