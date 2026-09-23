using Brezee.App.Connections;

namespace Brezee.App.Shell;

// Opens dialogs on behalf of view models, which must not reference windows directly.
public interface IDialogService
{
    // Shows the Connect dialog. Returns the new session, or null if the user cancelled.
    IDatabaseSession? ShowConnectDialog();
}
