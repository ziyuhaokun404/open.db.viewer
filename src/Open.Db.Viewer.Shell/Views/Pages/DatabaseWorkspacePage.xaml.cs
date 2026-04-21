using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using Open.Db.Viewer.Domain.Models;
using Open.Db.Viewer.Shell.ViewModels;

namespace Open.Db.Viewer.Shell.Views.Pages;

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

    private async void DataGridView_OnSorting(object sender, DataGridSortingEventArgs e)
    {
        e.Handled = true;

        if (DataContext is not DatabaseWorkspaceViewModel viewModel ||
            string.IsNullOrWhiteSpace(e.Column.SortMemberPath))
        {
            return;
        }

        await viewModel.Data.ApplySortAsync(e.Column.SortMemberPath);

        foreach (var column in ((DataGrid)sender).Columns)
        {
            column.SortDirection = null;
        }

        e.Column.SortDirection = string.Equals(viewModel.Data.SortDirection, "DESC", StringComparison.OrdinalIgnoreCase)
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;
    }

    private async void PageSizeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || DataContext is not DatabaseWorkspaceViewModel viewModel)
        {
            return;
        }

        if (e.AddedItems.Count == 0 || e.AddedItems[0] is not int pageSize)
        {
            return;
        }

        if (viewModel.Data.PageNumber == 0)
        {
            return;
        }

        await viewModel.Data.ChangePageSizeAsync(pageSize);
        viewModel.StatusMessage = $"每页行数已更新为 {pageSize}。";
    }
}
