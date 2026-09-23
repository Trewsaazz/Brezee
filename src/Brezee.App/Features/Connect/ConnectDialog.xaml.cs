using System.Windows;
using System.Windows.Controls;

namespace Brezee.App.Features.Connect;

public partial class ConnectDialog : Window
{
    private readonly ConnectDialogViewModel _viewModel;

    public ConnectDialog(ConnectDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = _viewModel = viewModel;
        _viewModel.Connected += (_, _) => DialogResult = true;
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // A connect attempt cannot be cancelled once started; closing now would leak its connection.
        if (_viewModel.IsBusy)
            e.Cancel = true;

        base.OnClosing(e);
    }

    private void OnPasswordChanged(object sender, RoutedEventArgs e) =>
        _viewModel.Password = ((PasswordBox)sender).Password;
}
