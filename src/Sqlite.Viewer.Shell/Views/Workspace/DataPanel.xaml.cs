using Sqlite.Viewer.Shell.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Sqlite.Viewer.Shell.Views.Workspace;

public partial class DataPanel : UserControl
{
    public DataPanel()
    {
        InitializeComponent();
    }

    private void OnAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        if (e.Column is DataGridTextColumn textColumn)
        {
            textColumn.ElementStyle = (Style)FindResource("DataGridTextCellStyle");
        }
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

    private void DataGridView_OnCurrentCellChanged(object? sender, EventArgs e)
    {
        if (DataContext is not DatabaseWorkspaceViewModel viewModel || sender is not DataGrid grid)
        {
            return;
        }

        if (grid.CurrentCell.Column is null)
        {
            viewModel.Data.SelectedColumnIndex = -1;
            return;
        }

        viewModel.Data.SelectedColumnIndex = grid.Columns.IndexOf(grid.CurrentCell.Column);
        if (grid.CurrentItem is not null)
        {
            viewModel.Data.SelectedRowIndex = grid.Items.IndexOf(grid.CurrentItem);
        }
    }
}
