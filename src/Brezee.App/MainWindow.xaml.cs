using System.Windows;
using Brezee.App.Shell;

namespace Brezee.App;

public partial class MainWindow : Window
{
    public MainWindow(ShellViewModel shell)
    {
        InitializeComponent();
        DataContext = shell;
    }
}
