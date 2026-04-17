using System.Windows;
using System.Windows.Controls;
using OpenDbViewer.Domain.Models;
using OpenDbViewer.Shell.ViewModels;

namespace OpenDbViewer.Shell.Views.Pages;

public partial class DatabaseWorkspacePage : UserControl
{
    public DatabaseWorkspacePage()
    {
        InitializeComponent();
    }

    private async void ObjectTree_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is not DatabaseWorkspaceViewModel viewModel ||
            e.NewValue is not DatabaseObjectNode node)
        {
            return;
        }

        await viewModel.SelectNodeAsync(node);
    }
}
