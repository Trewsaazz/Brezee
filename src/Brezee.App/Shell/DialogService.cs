using System.Windows;
using Brezee.App.Connections;
using Brezee.App.Features.Connect;

namespace Brezee.App.Shell;

public sealed class DialogService(IDatabaseConnector connector) : IDialogService
{
    public ConnectResult? ShowConnectDialog(SavedConnection? prefill)
    {
        var viewModel = new ConnectDialogViewModel(connector);
        if (prefill is not null)
            viewModel.LoadFrom(prefill);

        var dialog = new ConnectDialog(viewModel) { Owner = Application.Current.MainWindow };
        if (dialog.ShowDialog() != true || viewModel.Session is null)
            return null;

        return new ConnectResult(viewModel.Session, viewModel.CreateSavedConnection());
    }
}
