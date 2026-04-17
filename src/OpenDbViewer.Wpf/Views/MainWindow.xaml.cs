using Wpf.Ui.Controls;

namespace OpenDbViewer.Shell.Views;

public partial class MainWindow : FluentWindow
{
    public MainWindow(ViewModels.ShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
