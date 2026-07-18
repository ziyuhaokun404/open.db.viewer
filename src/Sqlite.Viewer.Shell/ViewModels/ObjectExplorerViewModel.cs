using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sqlite.Viewer.Domain.Models;
using Sqlite.Viewer.Infrastructure.Sqlite.Sqlite;
using System.Collections.ObjectModel;

namespace Sqlite.Viewer.Shell.ViewModels;

public partial class ObjectExplorerViewModel : ObservableObject
{
    private readonly SqliteDatabaseInspector? _databaseInspector;
    private IReadOnlyList<DatabaseObjectNode> _allNodes = Array.Empty<DatabaseObjectNode>();
    private string? _databasePath;

    [ObservableProperty]
    private DatabaseObjectNode? selectedNode;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool showSystemObjects;

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

    public int TotalObjectCount => _allNodes.Count(node => !node.IsGroup);

    public int FilteredObjectCount => FilteredTables.Count;

    public bool HasSearchText => !string.IsNullOrWhiteSpace(SearchText);

    public string ObjectCountSummary => string.IsNullOrWhiteSpace(SearchText)
        ? $"{TotalObjectCount} 个对象"
        : $"显示 {FilteredObjectCount} / {TotalObjectCount} 个对象";

    public string GroupTitle => $"对象 ({FilteredObjectCount})";

    public string FooterSummary => string.IsNullOrWhiteSpace(SearchText)
        ? $"共 {TotalObjectCount} 个对象"
        : $"筛选结果 {FilteredObjectCount} / {TotalObjectCount}";

    public async Task LoadAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        if (_databaseInspector is null)
        {
            throw new InvalidOperationException("A database inspector is required to load objects.");
        }

        _databasePath = databasePath;
        var selectedId = SelectedNode?.Id;
        _allNodes = await _databaseInspector.GetObjectCatalogAsync(databasePath, ShowSystemObjects, cancellationToken);
        ApplyFilter(selectedId);
    }

    [RelayCommand]
    public void ClearSearch() => SearchText = string.Empty;

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter(SelectedNode?.Id);
        OnPropertyChanged(nameof(HasSearchText));
    }

    partial void OnShowSystemObjectsChanged(bool value)
    {
        if (string.IsNullOrWhiteSpace(_databasePath) || _databaseInspector is null)
        {
            return;
        }

        _ = LoadAsync(_databasePath);
    }

    private void ApplyFilter(string? preferredSelectionId)
    {
        var filteredNodes = string.IsNullOrWhiteSpace(SearchText)
            ? _allNodes
            : _allNodes
                .Where(node =>
                    node.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    node.KindLabel.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    (node.ParentObjectName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToArray();

        FilteredTables.Clear();
        foreach (var node in filteredNodes)
        {
            FilteredTables.Add(node);
        }

        RootNodes.Clear();
        foreach (var group in BuildGroups(filteredNodes))
        {
            RootNodes.Add(group);
        }

        SelectedNode = filteredNodes.FirstOrDefault(node =>
                !string.IsNullOrWhiteSpace(preferredSelectionId) &&
                node.Id.Equals(preferredSelectionId, StringComparison.OrdinalIgnoreCase))
            ?? filteredNodes.FirstOrDefault(node => node.Kind.Equals(DatabaseObjectKinds.Table, StringComparison.OrdinalIgnoreCase))
            ?? filteredNodes.FirstOrDefault();

        OnPropertyChanged(nameof(HasFilteredTables));
        OnPropertyChanged(nameof(TotalObjectCount));
        OnPropertyChanged(nameof(FilteredObjectCount));
        OnPropertyChanged(nameof(HasSearchText));
        OnPropertyChanged(nameof(ObjectCountSummary));
        OnPropertyChanged(nameof(GroupTitle));
        OnPropertyChanged(nameof(FooterSummary));
    }

    private static IEnumerable<DatabaseObjectNode> BuildGroups(IReadOnlyList<DatabaseObjectNode> nodes)
    {
        yield return CreateGroup("tables", "表", DatabaseObjectKinds.Table, nodes);
        yield return CreateGroup("views", "视图", DatabaseObjectKinds.View, nodes);
        yield return CreateGroup("indexes", "索引", DatabaseObjectKinds.Index, nodes);
        yield return CreateGroup("triggers", "触发器", DatabaseObjectKinds.Trigger, nodes);
        yield return CreateGroup("system", "系统表", DatabaseObjectKinds.System, nodes);
    }

    private static DatabaseObjectNode CreateGroup(
        string idSuffix,
        string title,
        string kind,
        IReadOnlyList<DatabaseObjectNode> nodes)
    {
        var children = nodes.Where(node => node.Kind.Equals(kind, StringComparison.OrdinalIgnoreCase)).ToArray();
        return new DatabaseObjectNode(
            Id: $"group:{idSuffix}",
            Kind: DatabaseObjectKinds.Group,
            Name: $"{title} ({children.Length})",
            Children: children);
    }
}
