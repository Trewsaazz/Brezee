using Brezee.App.Connections;
using Brezee.Bridge;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Brezee.App.Features.Connect;

// The "Connect to Database" form. Connects on submit and stays open, showing the error, if it fails.
public sealed partial class ConnectDialogViewModel : ObservableObject
{
    private readonly IDatabaseConnector _connector;
    private readonly ICredentialProtector _protector;
    private SavedConnection? _prefill;

    public ConnectDialogViewModel(IDatabaseConnector connector, ICredentialProtector protector)
    {
        _connector = connector;
        _protector = protector;

        Host = "localhost";
        Port = 3050;
        Database = string.Empty;
        User = "SYSDBA";
        Password = string.Empty;
        Role = string.Empty;
        Charset = "UTF8";
        ConnectionName = string.Empty;
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

    // Whether to remember these details as a saved connection after connecting.
    [ObservableProperty]
    public partial bool SaveConnection { get; set; }

    // Name for the saved connection. Blank means "use the database file name".
    [ObservableProperty]
    public partial string ConnectionName { get; set; }

    // True when the dialog was opened for an existing saved connection.
    [ObservableProperty]
    public partial bool IsForSavedConnection { get; private set; }

    // Whether to keep the password, encrypted for the current Windows user.
    [ObservableProperty]
    public partial bool RememberPassword { get; set; }

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

    // Pre-fills the form from a saved connection; only the password is left to enter.
    public void LoadFrom(SavedConnection connection)
    {
        _prefill = connection;
        IsForSavedConnection = true;
        RememberPassword = connection.HasSavedPassword;
        ConnectionName = connection.Name;
        IsLocal = connection.IsLocal;
        Host = connection.Host;
        Port = connection.Port;
        Database = connection.Database;
        User = connection.User;
        Role = connection.Role;
        Charset = connection.Charset;
    }

    // What to store after a successful connect: a new saved connection if the user asked for one, an
    // update of the pre-filled one if its remembered password changed, or null. The password is only
    // ever included encrypted.
    public SavedConnection? CreateSavedConnection()
    {
        var protectedPassword = RememberPassword && Password.Length > 0 ? _protector.Protect(Password) : null;

        if (_prefill is { } prefill)
        {
            // Store the newly typed password, or forget the old one if the box was unticked.
            if (protectedPassword is not null || (!RememberPassword && prefill.HasSavedPassword))
                return prefill with { ProtectedPassword = protectedPassword };
            return null;
        }

        if (!SaveConnection)
            return null;

        var settings = ToSettings();
        return new SavedConnection
        {
            Name = string.IsNullOrWhiteSpace(ConnectionName) ? DatabaseNames.FromPath(settings.Database) : ConnectionName.Trim(),
            IsLocal = IsLocal,
            Host = settings.Host,
            Port = settings.Port,
            Database = settings.Database,
            User = settings.User,
            Role = settings.Role,
            Charset = settings.Charset,
            ProtectedPassword = protectedPassword,
        };
    }

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
