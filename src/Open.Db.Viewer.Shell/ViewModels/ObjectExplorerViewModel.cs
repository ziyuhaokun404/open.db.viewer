using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Open.Db.Viewer.Domain.Models;
using Open.Db.Viewer.Infrastructure.Sqlite.Sqlite;
using System.Collections.ObjectModel;

namespace Open.Db.Viewer.Shell.ViewModels;

public partial class ObjectExplorerViewModel : ObservableObject
{
    private readonly SqliteDatabaseInspector? _databaseInspector;
    private IReadOnlyList<DatabaseObjectNode> _allTableNodes = Array.Empty<DatabaseObjectNode>();

    [ObservableProperty]
    private DatabaseObjectNode? selectedNode;

    [ObservableProperty]
    private string searchText = string.Empty;

    public ObjectExplorerViewModel()
    {
    }

    public ObjectExplorerViewModel(SqliteDatabaseInspector databaseInspector)
    {
        _databaseInspector = databaseInspector;
    }

    public ObservableCollection<DatabaseObjectNode> RootNodes { get; } = new();

    public ObservableCollection<DatabaseObjectNode> FilteredTables { get; } = new();

    public bool HasFilteredTables => FilteredTables.Count > 0;

    public int TotalObjectCount => _allTableNodes.Count;

    public int FilteredObjectCount => FilteredTables.Count;

    public bool HasSearchText => !string.IsNullOrWhiteSpace(SearchText);

    public string ObjectCountSummary => string.IsNullOrWhiteSpace(SearchText)
        ? $"{TotalObjectCount} 个表"
        : $"显示 {FilteredObjectCount} / {TotalObjectCount} 个表";

    public string GroupTitle => $"表 ({FilteredObjectCount})";

    public string FooterSummary => string.IsNullOrWhiteSpace(SearchText)
        ? $"共 {TotalObjectCount} 个对象"
        : $"筛选结果 {FilteredObjectCount} / {TotalObjectCount}";

    public async Task LoadAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        if (_databaseInspector is null)
        {
            throw new InvalidOperationException("A database inspector is required to load objects.");
        }

        var tables = await _databaseInspector.GetTablesAsync(databasePath, cancellationToken);
        var selectedTableName = SelectedNode?.Name;

        _allTableNodes = tables
            .Select(tableName => new DatabaseObjectNode(
                Id: $"table:{tableName}",
                Kind: "table",
                Name: tableName,
                ParentId: "group:tables",
                Children: Array.Empty<DatabaseObjectNode>()))
            .ToArray();

        ApplyFilter(selectedTableName);
    }

    [RelayCommand]
    public void ClearSearch() => SearchText = string.Empty;

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter(SelectedNode?.Name);
        OnPropertyChanged(nameof(HasSearchText));
    }

    private void ApplyFilter(string? preferredSelectionName)
    {
        var filteredNodes = string.IsNullOrWhiteSpace(SearchText)
            ? _allTableNodes
            : _allTableNodes
                .Where(node => node.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .ToArray();

        FilteredTables.Clear();
        foreach (var node in filteredNodes)
        {
            FilteredTables.Add(node);
        }

        RootNodes.Clear();
        RootNodes.Add(new DatabaseObjectNode(
            Id: "group:tables",
            Kind: "group",
            Name: $"表 ({FilteredTables.Count})",
            Children: filteredNodes));

        SelectedNode = filteredNodes.FirstOrDefault(node =>
                !string.IsNullOrWhiteSpace(preferredSelectionName) &&
                node.Name.Equals(preferredSelectionName, StringComparison.OrdinalIgnoreCase))
            ?? filteredNodes.FirstOrDefault();

        OnPropertyChanged(nameof(HasFilteredTables));
        OnPropertyChanged(nameof(TotalObjectCount));
        OnPropertyChanged(nameof(FilteredObjectCount));
        OnPropertyChanged(nameof(HasSearchText));
        OnPropertyChanged(nameof(ObjectCountSummary));
        OnPropertyChanged(nameof(GroupTitle));
        OnPropertyChanged(nameof(FooterSummary));
    }
}
