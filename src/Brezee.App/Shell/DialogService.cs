using System.Windows;
using Brezee.App.Connections;
using Brezee.App.Features.Connect;

namespace Brezee.App.Shell;

public sealed class DialogService(IDatabaseConnector connector) : IDialogService
{
    public IDatabaseSession? ShowConnectDialog()
    {
        var viewModel = new ConnectDialogViewModel(connector);
        var dialog = new ConnectDialog(viewModel) { Owner = Application.Current.MainWindow };

        return dialog.ShowDialog() == true ? viewModel.Session : null;
    }
}
