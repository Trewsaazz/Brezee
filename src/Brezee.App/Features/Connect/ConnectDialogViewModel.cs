using Brezee.App.Connections;
using Brezee.Bridge;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Brezee.App.Features.Connect;

// The "Connect to Database" form. Connects on submit and stays open, showing the error, if it fails.
public sealed partial class ConnectDialogViewModel : ObservableObject
{
    private readonly IDatabaseConnector _connector;

    public ConnectDialogViewModel(IDatabaseConnector connector)
    {
        _connector = connector;

        Host = "localhost";
        Port = 3050;
        Database = string.Empty;
        User = "SYSDBA";
        Password = string.Empty;
        Role = string.Empty;
        Charset = "UTF8";
    }

    // True to open a database file on this computer with the embedded engine, no server needed.
    [ObservableProperty]
    public partial bool IsLocal { get; set; }

    [ObservableProperty]
    public partial string Host { get; set; }

    [ObservableProperty]
    public partial int Port { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    public partial string Database { get; set; }

    [ObservableProperty]
    public partial string User { get; set; }

    // Set from the view's PasswordBox, which cannot be data-bound.
    public string Password { get; set; }

    [ObservableProperty]
    public partial string Role { get; set; }

    [ObservableProperty]
    public partial string Charset { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    public partial bool IsBusy { get; set; }

    // Why the last attempt failed, in Firebird's words. Null when there is nothing to show.
    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    // The open session once Connect succeeds.
    public IDatabaseSession? Session { get; private set; }

    // Raised after a successful connect so the view can close.
    public event EventHandler? Connected;

    public ConnectionSettings ToSettings() => new()
    {
        Host = IsLocal ? string.Empty : Host.Trim(),
        Port = Port,
        Database = Database.Trim(),
        User = User.Trim(),
        Password = Password,
        Role = Role.Trim(),
        Charset = Charset.Trim(),
    };

    private bool CanConnect() => !IsBusy && !string.IsNullOrWhiteSpace(Database);

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            Session = await _connector.ConnectAsync(ToSettings());
            Connected?.Invoke(this, EventArgs.Empty);
        }
        catch (CoreException ex)
        {
            // Expected failures (wrong path, wrong password, server down): show them in the form.
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
