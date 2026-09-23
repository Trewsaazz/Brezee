using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Brezee.App.Features.Explorer;

public partial class ExplorerView : UserControl
{
    public ExplorerView()
    {
        InitializeComponent();
        Tree.PreviewKeyDown += OnPreviewKeyDown;
    }

    private ExplorerViewModel? ViewModel => DataContext as ExplorerViewModel;

    // TreeView.SelectedItem cannot be bound, so the selection is passed on here.
    private void OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (ViewModel is { } viewModel)
            viewModel.SelectedNode = e.NewValue as ExplorerNode;
    }

    // Double-clicking a disconnected database connects it.
    private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel is { } viewModel
            && Tree.SelectedItem is DatabaseNodeViewModel { IsConnected: false } database
            && viewModel.ConnectCommand.CanExecute(database))
        {
            viewModel.ConnectCommand.Execute(database);
            e.Handled = true;
        }
    }

    // Enter on a disconnected database connects it (elsewhere it keeps its usual meaning).
    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter
            && ViewModel is { } viewModel
            && Tree.SelectedItem is DatabaseNodeViewModel { IsConnected: false } database
            && viewModel.ConnectCommand.CanExecute(database))
        {
            viewModel.ConnectCommand.Execute(database);
            e.Handled = true;
        }
    }
}
