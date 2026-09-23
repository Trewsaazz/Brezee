using System.Windows;
using Brezee.Bridge;

namespace Brezee.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Proves the whole chain works: WPF -> C++/CLI bridge -> native core.
        CoreVersionText.Text = $"Core v{CoreInfo.Version}";
    }
}
