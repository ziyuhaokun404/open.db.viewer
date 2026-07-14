using Open.Db.Viewer.Domain.Models;
using Open.Db.Viewer.Shell.ViewModels;
using System.Windows.Controls;

namespace Open.Db.Viewer.Shell.Views.Workspace;

public partial class ObjectExplorerPanel : UserControl
{
    public ObjectExplorerPanel()
    {
        InitializeComponent();
    }

    private async void ObjectList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not DatabaseWorkspaceViewModel viewModel ||
            e.AddedItems.Count == 0 ||
            e.AddedItems[0] is not DatabaseObjectNode node)
        {
            return;
        }

        if (string.Equals(viewModel.ObjectExplorer.SelectedNode?.Id, node.Id, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(viewModel.Schema.TableName, node.Name, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await viewModel.SelectNodeAsync(node);
    }
}
