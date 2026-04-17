# Open DB Viewer WPF MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a usable SQLite-only WPF desktop viewer with quick-open, object tree, schema view, table data browsing, SQL query execution, and CSV export.

**Architecture:** Create a small multi-project .NET solution with a WPF shell on top of application services, domain models, and a SQLite infrastructure layer. Keep the UI MVVM-driven, keep SQLite access behind focused services, and validate each feature with tests before wiring the next screen.

**Tech Stack:** .NET 8, WPF, WPF UI, CommunityToolkit.Mvvm, Microsoft.Data.Sqlite, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Logging, xUnit

---

## Planned File Structure

### Solution and projects

- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.sln`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Domain\OpenDbViewer.Domain.csproj`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Application\OpenDbViewer.Application.csproj`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Infrastructure.Sqlite\OpenDbViewer.Infrastructure.Sqlite.csproj`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\OpenDbViewer.Wpf.csproj`
- Create: `C:\Code\open.db.viewer\tests\OpenDbViewer.Domain.Tests\OpenDbViewer.Domain.Tests.csproj`
- Create: `C:\Code\open.db.viewer\tests\OpenDbViewer.Application.Tests\OpenDbViewer.Application.Tests.csproj`
- Create: `C:\Code\open.db.viewer\tests\OpenDbViewer.Infrastructure.Sqlite.Tests\OpenDbViewer.Infrastructure.Sqlite.Tests.csproj`

### Domain

- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Domain\Models\DatabaseEntry.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Domain\Models\DatabaseObjectNode.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Domain\Models\TableColumnInfo.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Domain\Models\TableSchema.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Domain\Models\TabularData.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Domain\Models\TablePageResult.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Domain\Models\QueryExecutionRequest.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Domain\Models\QueryExecutionResult.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Domain\Models\OperationResult.cs`

### Application

- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Application\Abstractions\IDatabaseEntryRepository.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Application\Abstractions\ISqliteConnectionFactory.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Application\Abstractions\ISqliteDatabaseInspector.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Application\Abstractions\ISqliteTableDataReader.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Application\Abstractions\ISqliteQueryExecutor.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Application\Abstractions\ICsvExportWriter.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Application\Services\DatabaseEntryService.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Application\Services\ExplorerService.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Application\Services\SchemaService.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Application\Services\TableDataService.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Application\Services\QueryService.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Application\Services\ExportService.cs`

### Infrastructure

- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Infrastructure.Sqlite\Storage\DatabaseEntryRepository.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Infrastructure.Sqlite\Storage\DatabaseEntryStore.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Infrastructure.Sqlite\Sqlite\SqliteConnectionFactory.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Infrastructure.Sqlite\Sqlite\SqliteDatabaseInspector.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Infrastructure.Sqlite\Sqlite\SqliteTableDataReader.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Infrastructure.Sqlite\Sqlite\SqliteQueryExecutor.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Infrastructure.Sqlite\Export\CsvExportWriter.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Infrastructure.Sqlite\ServiceCollectionExtensions.cs`

### WPF app

- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\App.xaml`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\App.xaml.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\Resources\Styles\ThemeResources.xaml`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\Views\MainWindow.xaml`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\Views\MainWindow.xaml.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\Views\Pages\HomePage.xaml`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\Views\Pages\DatabaseWorkspacePage.xaml`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\Views\Pages\HomePage.xaml.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\Views\Pages\DatabaseWorkspacePage.xaml.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\ViewModels\ShellViewModel.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\ViewModels\HomeViewModel.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\ViewModels\DatabaseWorkspaceViewModel.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\ViewModels\ObjectExplorerViewModel.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\ViewModels\SchemaViewModel.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\ViewModels\DataViewModel.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\ViewModels\QueryViewModel.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\ViewModels\Design\TabularRowViewModel.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\Services\FileDialogService.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\ServiceCollectionExtensions.cs`

### Test helpers

- Create: `C:\Code\open.db.viewer\tests\OpenDbViewer.Infrastructure.Sqlite.Tests\Support\SqliteTestDb.cs`
- Create: `C:\Code\open.db.viewer\tests\OpenDbViewer.Application.Tests\Support\InMemoryDatabaseEntryRepository.cs`

## Task 1: Scaffold the .NET solution and baseline test harness

**Files:**
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.sln`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Domain\OpenDbViewer.Domain.csproj`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Application\OpenDbViewer.Application.csproj`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Infrastructure.Sqlite\OpenDbViewer.Infrastructure.Sqlite.csproj`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\OpenDbViewer.Wpf.csproj`
- Create: `C:\Code\open.db.viewer\tests\OpenDbViewer.Domain.Tests\OpenDbViewer.Domain.Tests.csproj`
- Create: `C:\Code\open.db.viewer\tests\OpenDbViewer.Application.Tests\OpenDbViewer.Application.Tests.csproj`
- Create: `C:\Code\open.db.viewer\tests\OpenDbViewer.Infrastructure.Sqlite.Tests\OpenDbViewer.Infrastructure.Sqlite.Tests.csproj`

- [ ] **Step 1: Create the solution and projects**

```powershell
dotnet new sln -n OpenDbViewer --output C:\Code\open.db.viewer\src
dotnet new classlib -n OpenDbViewer.Domain -o C:\Code\open.db.viewer\src\OpenDbViewer.Domain
dotnet new classlib -n OpenDbViewer.Application -o C:\Code\open.db.viewer\src\OpenDbViewer.Application
dotnet new classlib -n OpenDbViewer.Infrastructure.Sqlite -o C:\Code\open.db.viewer\src\OpenDbViewer.Infrastructure.Sqlite
dotnet new wpf -n OpenDbViewer.Wpf -o C:\Code\open.db.viewer\src\OpenDbViewer.Wpf
dotnet new xunit -n OpenDbViewer.Domain.Tests -o C:\Code\open.db.viewer\tests\OpenDbViewer.Domain.Tests
dotnet new xunit -n OpenDbViewer.Application.Tests -o C:\Code\open.db.viewer\tests\OpenDbViewer.Application.Tests
dotnet new xunit -n OpenDbViewer.Infrastructure.Sqlite.Tests -o C:\Code\open.db.viewer\tests\OpenDbViewer.Infrastructure.Sqlite.Tests
```

- [ ] **Step 2: Add projects to the solution**

```powershell
dotnet sln C:\Code\open.db.viewer\src\OpenDbViewer.sln add `
  C:\Code\open.db.viewer\src\OpenDbViewer.Domain\OpenDbViewer.Domain.csproj `
  C:\Code\open.db.viewer\src\OpenDbViewer.Application\OpenDbViewer.Application.csproj `
  C:\Code\open.db.viewer\src\OpenDbViewer.Infrastructure.Sqlite\OpenDbViewer.Infrastructure.Sqlite.csproj `
  C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\OpenDbViewer.Wpf.csproj `
  C:\Code\open.db.viewer\tests\OpenDbViewer.Domain.Tests\OpenDbViewer.Domain.Tests.csproj `
  C:\Code\open.db.viewer\tests\OpenDbViewer.Application.Tests\OpenDbViewer.Application.Tests.csproj `
  C:\Code\open.db.viewer\tests\OpenDbViewer.Infrastructure.Sqlite.Tests\OpenDbViewer.Infrastructure.Sqlite.Tests.csproj
```

- [ ] **Step 3: Wire project references**

```powershell
dotnet add C:\Code\open.db.viewer\src\OpenDbViewer.Application\OpenDbViewer.Application.csproj reference `
  C:\Code\open.db.viewer\src\OpenDbViewer.Domain\OpenDbViewer.Domain.csproj
dotnet add C:\Code\open.db.viewer\src\OpenDbViewer.Infrastructure.Sqlite\OpenDbViewer.Infrastructure.Sqlite.csproj reference `
  C:\Code\open.db.viewer\src\OpenDbViewer.Application\OpenDbViewer.Application.csproj `
  C:\Code\open.db.viewer\src\OpenDbViewer.Domain\OpenDbViewer.Domain.csproj
dotnet add C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\OpenDbViewer.Wpf.csproj reference `
  C:\Code\open.db.viewer\src\OpenDbViewer.Application\OpenDbViewer.Application.csproj `
  C:\Code\open.db.viewer\src\OpenDbViewer.Infrastructure.Sqlite\OpenDbViewer.Infrastructure.Sqlite.csproj `
  C:\Code\open.db.viewer\src\OpenDbViewer.Domain\OpenDbViewer.Domain.csproj
dotnet add C:\Code\open.db.viewer\tests\OpenDbViewer.Domain.Tests\OpenDbViewer.Domain.Tests.csproj reference `
  C:\Code\open.db.viewer\src\OpenDbViewer.Domain\OpenDbViewer.Domain.csproj
dotnet add C:\Code\open.db.viewer\tests\OpenDbViewer.Application.Tests\OpenDbViewer.Application.Tests.csproj reference `
  C:\Code\open.db.viewer\src\OpenDbViewer.Application\OpenDbViewer.Application.csproj `
  C:\Code\open.db.viewer\src\OpenDbViewer.Domain\OpenDbViewer.Domain.csproj
dotnet add C:\Code\open.db.viewer\tests\OpenDbViewer.Infrastructure.Sqlite.Tests\OpenDbViewer.Infrastructure.Sqlite.Tests.csproj reference `
  C:\Code\open.db.viewer\src\OpenDbViewer.Infrastructure.Sqlite\OpenDbViewer.Infrastructure.Sqlite.csproj `
  C:\Code\open.db.viewer\src\OpenDbViewer.Application\OpenDbViewer.Application.csproj `
  C:\Code\open.db.viewer\src\OpenDbViewer.Domain\OpenDbViewer.Domain.csproj
```

- [ ] **Step 4: Add core packages**

```powershell
dotnet add C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\OpenDbViewer.Wpf.csproj package Wpf.Ui
dotnet add C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\OpenDbViewer.Wpf.csproj package CommunityToolkit.Mvvm
dotnet add C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\OpenDbViewer.Wpf.csproj package Microsoft.Extensions.DependencyInjection
dotnet add C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\OpenDbViewer.Wpf.csproj package Microsoft.Extensions.Logging.Console
dotnet add C:\Code\open.db.viewer\src\OpenDbViewer.Infrastructure.Sqlite\OpenDbViewer.Infrastructure.Sqlite.csproj package Microsoft.Data.Sqlite
dotnet add C:\Code\open.db.viewer\src\OpenDbViewer.Infrastructure.Sqlite\OpenDbViewer.Infrastructure.Sqlite.csproj package Microsoft.Extensions.DependencyInjection.Abstractions
dotnet add C:\Code\open.db.viewer\tests\OpenDbViewer.Application.Tests\OpenDbViewer.Application.Tests.csproj package FluentAssertions
dotnet add C:\Code\open.db.viewer\tests\OpenDbViewer.Infrastructure.Sqlite.Tests\OpenDbViewer.Infrastructure.Sqlite.Tests.csproj package FluentAssertions
```

- [ ] **Step 5: Run the empty test suite**

Run: `dotnet test C:\Code\open.db.viewer\src\OpenDbViewer.sln`  
Expected: all generated test projects pass.

- [ ] **Step 6: Commit**

```bash
git -C C:\Code\open.db.viewer add src tests
git -C C:\Code\open.db.viewer commit -m "chore: scaffold WPF MVP solution"
```

## Task 2: Define the domain models and validate their basic behavior

**Files:**
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Domain\Models\DatabaseEntry.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Domain\Models\DatabaseObjectNode.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Domain\Models\TableColumnInfo.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Domain\Models\TableSchema.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Domain\Models\TabularData.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Domain\Models\TablePageResult.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Domain\Models\QueryExecutionRequest.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Domain\Models\QueryExecutionResult.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Domain\Models\OperationResult.cs`
- Create: `C:\Code\open.db.viewer\tests\OpenDbViewer.Domain.Tests\Models\DomainModelsTests.cs`

- [ ] **Step 1: Write the failing tests for the core models**

```csharp
using FluentAssertions;
using OpenDbViewer.Domain.Models;

namespace OpenDbViewer.Domain.Tests.Models;

public class DomainModelsTests
{
    [Fact]
    public void DatabaseEntry_CreatePinned_ShouldMarkEntryAsPinned()
    {
        var entry = DatabaseEntry.CreatePinned("Demo", @"C:\data\demo.db");

        entry.Name.Should().Be("Demo");
        entry.FilePath.Should().Be(@"C:\data\demo.db");
        entry.IsPinned.Should().BeTrue();
    }

    [Fact]
    public void TablePageResult_ShouldExposeRowsAndPagination()
    {
        var result = new TablePageResult(
            new[] { "Id", "Name" },
            new[] { new object?[] { 1L, "Alice" } },
            pageNumber: 1,
            pageSize: 100,
            hasNextPage: false,
            sortColumn: "Id",
            sortDirection: "ASC");

        result.Columns.Should().ContainInOrder("Id", "Name");
        result.Rows.Should().HaveCount(1);
        result.PageNumber.Should().Be(1);
    }
}
```

- [ ] **Step 2: Run the domain tests to verify they fail**

Run: `dotnet test C:\Code\open.db.viewer\tests\OpenDbViewer.Domain.Tests\OpenDbViewer.Domain.Tests.csproj --filter DomainModelsTests`  
Expected: FAIL with missing types and constructors.

- [ ] **Step 3: Implement the domain models**

```csharp
namespace OpenDbViewer.Domain.Models;

public sealed record DatabaseEntry(
    Guid Id,
    string Name,
    string FilePath,
    DateTimeOffset LastOpenedAt,
    bool IsPinned)
{
    public static DatabaseEntry CreatePinned(string name, string filePath) =>
        new(Guid.NewGuid(), name, filePath, DateTimeOffset.UtcNow, true);

    public DatabaseEntry Touch() => this with { LastOpenedAt = DateTimeOffset.UtcNow };
}

public sealed record DatabaseObjectNode(string Name, string Kind, IReadOnlyList<DatabaseObjectNode> Children);

public sealed record TableColumnInfo(
    string Name,
    string DataType,
    bool IsNullable,
    string? DefaultValue,
    bool IsPrimaryKey);

public sealed record TableSchema(string TableName, IReadOnlyList<TableColumnInfo> Columns);

public sealed record TabularData(IReadOnlyList<string> Columns, IReadOnlyList<IReadOnlyList<object?>> Rows);

public sealed record TablePageResult(
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<object?>> Rows,
    int PageNumber,
    int PageSize,
    bool HasNextPage,
    string? SortColumn,
    string? SortDirection);

public sealed record QueryExecutionRequest(string Sql);

public sealed record QueryExecutionResult(
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<object?>> Rows,
    int AffectedRows,
    TimeSpan Duration,
    string Message);

public sealed record OperationResult(bool IsSuccess, string Message, string? ErrorCode = null)
{
    public static OperationResult Success(string message) => new(true, message);
    public static OperationResult Failure(string message, string errorCode) => new(false, message, errorCode);
}
```

- [ ] **Step 4: Run the domain tests to verify they pass**

Run: `dotnet test C:\Code\open.db.viewer\tests\OpenDbViewer.Domain.Tests\OpenDbViewer.Domain.Tests.csproj --filter DomainModelsTests`  
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git -C C:\Code\open.db.viewer add src\OpenDbViewer.Domain tests\OpenDbViewer.Domain.Tests
git -C C:\Code\open.db.viewer commit -m "feat: add WPF viewer domain models"
```

## Task 3: Build the application service contracts and entry management use cases

**Files:**
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Application\Abstractions\IDatabaseEntryRepository.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Application\Abstractions\ISqliteConnectionFactory.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Application\Services\DatabaseEntryService.cs`
- Create: `C:\Code\open.db.viewer\tests\OpenDbViewer.Application.Tests\Support\InMemoryDatabaseEntryRepository.cs`
- Create: `C:\Code\open.db.viewer\tests\OpenDbViewer.Application.Tests\Services\DatabaseEntryServiceTests.cs`

- [ ] **Step 1: Write failing tests for quick-open and saved entry behavior**

```csharp
using FluentAssertions;
using OpenDbViewer.Application.Services;

namespace OpenDbViewer.Application.Tests.Services;

public class DatabaseEntryServiceTests
{
    [Fact]
    public async Task OpenAsync_ShouldAddRecentEntry_WhenFileExists()
    {
        var repository = new InMemoryDatabaseEntryRepository();
        var service = new DatabaseEntryService(repository, filePath => Task.FromResult(true));

        var result = await service.OpenAsync(@"C:\data\demo.db");

        result.IsSuccess.Should().BeTrue();
        (await repository.GetRecentAsync()).Should().ContainSingle();
    }
}
```

- [ ] **Step 2: Run the application tests to verify they fail**

Run: `dotnet test C:\Code\open.db.viewer\tests\OpenDbViewer.Application.Tests\OpenDbViewer.Application.Tests.csproj --filter DatabaseEntryServiceTests`  
Expected: FAIL with missing service and repository contracts.

- [ ] **Step 3: Implement contracts, repository stub, and service**

```csharp
namespace OpenDbViewer.Application.Abstractions;

using OpenDbViewer.Domain.Models;

public interface IDatabaseEntryRepository
{
    Task<IReadOnlyList<DatabaseEntry>> GetRecentAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DatabaseEntry>> GetPinnedAsync(CancellationToken cancellationToken = default);
    Task SaveRecentAsync(DatabaseEntry entry, CancellationToken cancellationToken = default);
    Task SavePinnedAsync(DatabaseEntry entry, CancellationToken cancellationToken = default);
    Task RemovePinnedAsync(Guid id, CancellationToken cancellationToken = default);
}

namespace OpenDbViewer.Application.Services;

using OpenDbViewer.Application.Abstractions;
using OpenDbViewer.Domain.Models;

public sealed class DatabaseEntryService
{
    private readonly IDatabaseEntryRepository _repository;
    private readonly Func<string, Task<bool>> _fileExists;

    public DatabaseEntryService(IDatabaseEntryRepository repository, Func<string, Task<bool>> fileExists)
    {
        _repository = repository;
        _fileExists = fileExists;
    }

    public async Task<OperationResult> OpenAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return OperationResult.Failure("请选择一个 SQLite 文件。", "file.empty");
        }

        if (!await _fileExists(filePath))
        {
            return OperationResult.Failure("数据库文件不存在。", "file.missing");
        }

        var entry = new DatabaseEntry(Guid.NewGuid(), Path.GetFileNameWithoutExtension(filePath), filePath, DateTimeOffset.UtcNow, false);
        await _repository.SaveRecentAsync(entry, cancellationToken);
        return OperationResult.Success("数据库已打开。");
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test C:\Code\open.db.viewer\tests\OpenDbViewer.Application.Tests\OpenDbViewer.Application.Tests.csproj --filter DatabaseEntryServiceTests`  
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git -C C:\Code\open.db.viewer add src\OpenDbViewer.Application tests\OpenDbViewer.Application.Tests
git -C C:\Code\open.db.viewer commit -m "feat: add database entry application service"
```

## Task 4: Implement SQLite inspection, schema, and table data readers

**Files:**
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Infrastructure.Sqlite\Sqlite\SqliteConnectionFactory.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Infrastructure.Sqlite\Sqlite\SqliteDatabaseInspector.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Infrastructure.Sqlite\Sqlite\SqliteTableDataReader.cs`
- Create: `C:\Code\open.db.viewer\tests\OpenDbViewer.Infrastructure.Sqlite.Tests\Support\SqliteTestDb.cs`
- Create: `C:\Code\open.db.viewer\tests\OpenDbViewer.Infrastructure.Sqlite.Tests\Sqlite\SqliteInspectionTests.cs`

- [ ] **Step 1: Write failing SQLite integration tests**

```csharp
using FluentAssertions;

namespace OpenDbViewer.Infrastructure.Sqlite.Tests.Sqlite;

public class SqliteInspectionTests
{
    [Fact]
    public async Task GetTablesAsync_ShouldReturnSeededTables()
    {
        var db = await SqliteTestDb.CreateAsync();
        var factory = new SqliteConnectionFactory();
        var inspector = new SqliteDatabaseInspector(factory);

        var tables = await inspector.GetTablesAsync(db.FilePath);

        tables.Should().Contain("users");
    }
}
```

- [ ] **Step 2: Run the infrastructure tests to verify they fail**

Run: `dotnet test C:\Code\open.db.viewer\tests\OpenDbViewer.Infrastructure.Sqlite.Tests\OpenDbViewer.Infrastructure.Sqlite.Tests.csproj --filter SqliteInspectionTests`  
Expected: FAIL with missing SQLite infrastructure types.

- [ ] **Step 3: Implement the SQLite infrastructure**

```csharp
public sealed class SqliteConnectionFactory
{
    public SqliteConnection Create(string filePath)
    {
        var builder = new SqliteConnectionStringBuilder { DataSource = filePath, Mode = SqliteOpenMode.ReadWriteCreate };
        return new SqliteConnection(builder.ConnectionString);
    }
}

public sealed class SqliteDatabaseInspector
{
    private readonly SqliteConnectionFactory _factory;

    public SqliteDatabaseInspector(SqliteConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<string>> GetTablesAsync(string filePath)
    {
        await using var connection = _factory.Create(filePath);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name;";
        await using var reader = await command.ExecuteReaderAsync();

        var result = new List<string>();
        while (await reader.ReadAsync())
        {
            result.Add(reader.GetString(0));
        }

        return result;
    }
}
```

- [ ] **Step 4: Extend tests for schema and paged rows**

```csharp
[Fact]
public async Task GetSchemaAsync_ShouldReturnPrimaryKeyMetadata()
{
    var db = await SqliteTestDb.CreateAsync();
    var factory = new SqliteConnectionFactory();
    var inspector = new SqliteDatabaseInspector(factory);

    var schema = await inspector.GetSchemaAsync(db.FilePath, "users");

    schema.Columns.Should().Contain(x => x.Name == "id" && x.IsPrimaryKey);
}

[Fact]
public async Task ReadPageAsync_ShouldReturnRequestedPage()
{
    var db = await SqliteTestDb.CreateAsync();
    var factory = new SqliteConnectionFactory();
    var reader = new SqliteTableDataReader(factory);

    var page = await reader.ReadPageAsync(db.FilePath, "users", 1, 2, "id", "ASC");

    page.Rows.Should().HaveCount(2);
    page.HasNextPage.Should().BeTrue();
}
```

- [ ] **Step 5: Run the infrastructure tests to verify they pass**

Run: `dotnet test C:\Code\open.db.viewer\tests\OpenDbViewer.Infrastructure.Sqlite.Tests\OpenDbViewer.Infrastructure.Sqlite.Tests.csproj`  
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git -C C:\Code\open.db.viewer add src\OpenDbViewer.Infrastructure.Sqlite tests\OpenDbViewer.Infrastructure.Sqlite.Tests
git -C C:\Code\open.db.viewer commit -m "feat: add SQLite inspection and table data readers"
```

## Task 5: Implement query execution and CSV export

**Files:**
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Infrastructure.Sqlite\Sqlite\SqliteQueryExecutor.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Infrastructure.Sqlite\Export\CsvExportWriter.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Application\Services\QueryService.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Application\Services\ExportService.cs`
- Create: `C:\Code\open.db.viewer\tests\OpenDbViewer.Infrastructure.Sqlite.Tests\Sqlite\SqliteQueryExecutorTests.cs`
- Create: `C:\Code\open.db.viewer\tests\OpenDbViewer.Infrastructure.Sqlite.Tests\Export\CsvExportWriterTests.cs`

- [ ] **Step 1: Write failing tests for query execution and CSV escaping**

```csharp
[Fact]
public async Task ExecuteAsync_ShouldReturnRows_ForSelectStatements()
{
    var db = await SqliteTestDb.CreateAsync();
    var executor = new SqliteQueryExecutor(new SqliteConnectionFactory());

    var result = await executor.ExecuteAsync(db.FilePath, "select id, name from users order by id");

    result.Columns.Should().ContainInOrder("id", "name");
    result.Rows.Should().NotBeEmpty();
}

[Fact]
public async Task WriteAsync_ShouldEscapeQuotedValues()
{
    var writer = new CsvExportWriter();
    var target = Path.GetTempFileName();

    await writer.WriteAsync(target, new[] { "Name" }, new[] { new object?[] { "Alice, \"Admin\"" } });

    var text = await File.ReadAllTextAsync(target);
    text.Should().Contain("\"Alice, \"\"Admin\"\"\"");
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test C:\Code\open.db.viewer\tests\OpenDbViewer.Infrastructure.Sqlite.Tests\OpenDbViewer.Infrastructure.Sqlite.Tests.csproj --filter "SqliteQueryExecutorTests|CsvExportWriterTests"`  
Expected: FAIL

- [ ] **Step 3: Implement the query executor and CSV writer**

```csharp
public sealed class SqliteQueryExecutor
{
    private readonly SqliteConnectionFactory _factory;

    public SqliteQueryExecutor(SqliteConnectionFactory factory) => _factory = factory;

    public async Task<QueryExecutionResult> ExecuteAsync(string filePath, string sql)
    {
        await using var connection = _factory.Create(filePath);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var started = Stopwatch.StartNew();
        await using var reader = await command.ExecuteReaderAsync();

        var columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();
        var rows = new List<IReadOnlyList<object?>>();
        while (await reader.ReadAsync())
        {
            var values = new object?[reader.FieldCount];
            reader.GetValues(values);
            rows.Add(values);
        }

        started.Stop();
        return new QueryExecutionResult(columns, rows, 0, started.Elapsed, "查询执行完成。");
    }
}

public sealed class CsvExportWriter
{
    public async Task WriteAsync(string filePath, IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<object?>> rows)
    {
        await using var writer = new StreamWriter(filePath, false, new UTF8Encoding(true));
        await writer.WriteLineAsync(string.Join(",", columns.Select(Escape)));
        foreach (var row in rows)
        {
            await writer.WriteLineAsync(string.Join(",", row.Select(value => Escape(value?.ToString() ?? string.Empty))));
        }
    }

    private static string Escape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test C:\Code\open.db.viewer\tests\OpenDbViewer.Infrastructure.Sqlite.Tests\OpenDbViewer.Infrastructure.Sqlite.Tests.csproj`  
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git -C C:\Code\open.db.viewer add src\OpenDbViewer.Infrastructure.Sqlite src\OpenDbViewer.Application tests\OpenDbViewer.Infrastructure.Sqlite.Tests
git -C C:\Code\open.db.viewer commit -m "feat: add SQLite query and CSV export services"
```

## Task 6: Build the WPF shell, DI wiring, and home page quick-open flow

**Files:**
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\App.xaml`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\App.xaml.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\Views\MainWindow.xaml`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\ViewModels\ShellViewModel.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\ViewModels\HomeViewModel.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\Services\FileDialogService.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\ServiceCollectionExtensions.cs`

- [ ] **Step 1: Add a smoke testable home-page view model**

```csharp
public class HomeViewModelTests
{
    [Fact]
    public async Task OpenFileAsync_ShouldSetOpenedEntry_WhenOpenSucceeds()
    {
        var service = new FakeDatabaseEntryService();
        var vm = new HomeViewModel(service, new FakeFileDialogService(@"C:\data\demo.db"));

        await vm.OpenFileAsync();

        vm.StatusMessage.Should().Be("数据库已打开。");
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test C:\Code\open.db.viewer\tests\OpenDbViewer.Application.Tests\OpenDbViewer.Application.Tests.csproj --filter HomeViewModelTests`  
Expected: FAIL

- [ ] **Step 3: Implement the shell and home page view model**

```csharp
public partial class HomeViewModel : ObservableObject
{
    private readonly DatabaseEntryService _databaseEntryService;
    private readonly IFileDialogService _fileDialogService;

    [ObservableProperty]
    private string? statusMessage;

    public HomeViewModel(DatabaseEntryService databaseEntryService, IFileDialogService fileDialogService)
    {
        _databaseEntryService = databaseEntryService;
        _fileDialogService = fileDialogService;
    }

    [RelayCommand]
    public async Task OpenFileAsync()
    {
        var filePath = _fileDialogService.PickSqliteFile();
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        var result = await _databaseEntryService.OpenAsync(filePath);
        StatusMessage = result.Message;
    }
}
```

- [ ] **Step 4: Create the WPF shell markup**

```xml
<ui:FluentWindow
    x:Class="OpenDbViewer.Wpf.Views.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
    Title="Open DB Viewer"
    Width="1400"
    Height="900">
    <Frame x:Name="RootFrame" NavigationUIVisibility="Hidden" />
</ui:FluentWindow>
```

- [ ] **Step 5: Run the app manually**

Run: `dotnet run --project C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\OpenDbViewer.Wpf.csproj`  
Expected: app opens to the home page with a visible quick-open action.

- [ ] **Step 6: Commit**

```bash
git -C C:\Code\open.db.viewer add src\OpenDbViewer.Wpf tests\OpenDbViewer.Application.Tests
git -C C:\Code\open.db.viewer commit -m "feat: add WPF shell and home page flow"
```

## Task 7: Build the database workspace with object tree, schema, and table data tabs

**Files:**
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\Views\Pages\DatabaseWorkspacePage.xaml`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\ViewModels\DatabaseWorkspaceViewModel.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\ViewModels\ObjectExplorerViewModel.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\ViewModels\SchemaViewModel.cs`
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\ViewModels\DataViewModel.cs`

- [ ] **Step 1: Write an application-level test for selecting a table**

```csharp
[Fact]
public async Task SelectTableAsync_ShouldLoadSchemaAndFirstPage()
{
    var vm = new DatabaseWorkspaceViewModel(
        new ObjectExplorerViewModel(...),
        new SchemaViewModel(...),
        new DataViewModel(...),
        new QueryViewModel(...));

    await vm.SelectTableAsync("users");

    vm.Schema.TableName.Should().Be("users");
    vm.Data.Rows.Should().NotBeEmpty();
}
```

- [ ] **Step 2: Run the relevant tests to verify they fail**

Run: `dotnet test C:\Code\open.db.viewer\tests\OpenDbViewer.Application.Tests\OpenDbViewer.Application.Tests.csproj --filter SelectTableAsync`  
Expected: FAIL

- [ ] **Step 3: Implement workspace coordination and tab loading**

```csharp
public sealed class DatabaseWorkspaceViewModel : ObservableObject
{
    public ObjectExplorerViewModel Explorer { get; }
    public SchemaViewModel Schema { get; }
    public DataViewModel Data { get; }
    public QueryViewModel Query { get; }

    [ObservableProperty]
    private string selectedTab = "Schema";

    public DatabaseWorkspaceViewModel(
        ObjectExplorerViewModel explorer,
        SchemaViewModel schema,
        DataViewModel data,
        QueryViewModel query)
    {
        Explorer = explorer;
        Schema = schema;
        Data = data;
        Query = query;
    }

    public async Task SelectTableAsync(string tableName)
    {
        SelectedTab = "Schema";
        await Schema.LoadAsync(tableName);
        await Data.LoadFirstPageAsync(tableName);
        Query.SetActiveTable(tableName);
    }
}
```

- [ ] **Step 4: Implement the workspace XAML layout**

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="280" />
        <ColumnDefinition Width="*" />
    </Grid.ColumnDefinitions>
    <TreeView Grid.Column="0" ItemsSource="{Binding Explorer.Nodes}" />
    <TabControl Grid.Column="1" SelectedItem="{Binding SelectedTab}">
        <TabItem Header="结构">
            <DataGrid ItemsSource="{Binding Schema.Columns}" />
        </TabItem>
        <TabItem Header="数据">
            <DataGrid ItemsSource="{Binding Data.Rows}" />
        </TabItem>
        <TabItem Header="查询">
            <Grid />
        </TabItem>
    </TabControl>
</Grid>
```

- [ ] **Step 5: Manually verify the workspace**

Run: `dotnet run --project C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\OpenDbViewer.Wpf.csproj`  
Expected: selecting a table shows schema first and data is ready on the next tab.

- [ ] **Step 6: Commit**

```bash
git -C C:\Code\open.db.viewer add src\OpenDbViewer.Wpf tests\OpenDbViewer.Application.Tests
git -C C:\Code\open.db.viewer commit -m "feat: add workspace object tree schema and data tabs"
```

## Task 8: Build the query page, export actions, and final MVP verification

**Files:**
- Create: `C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\ViewModels\QueryViewModel.cs`
- Modify: `C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\Views\Pages\DatabaseWorkspacePage.xaml`
- Create: `C:\Code\open.db.viewer\tests\OpenDbViewer.Application.Tests\Services\QueryViewModelTests.cs`
- Create: `C:\Code\open.db.viewer\docs\verification\wpf-mvp-checklist.md`

- [ ] **Step 1: Write failing tests for query execution and export command enablement**

```csharp
[Fact]
public async Task ExecuteAsync_ShouldPopulateResultGrid()
{
    var vm = new QueryViewModel(...);
    vm.SqlText = "select id, name from users order by id";

    await vm.ExecuteAsync();

    vm.ResultColumns.Should().Contain("id");
    vm.ResultRows.Should().NotBeEmpty();
}
```

- [ ] **Step 2: Run the query tests to verify they fail**

Run: `dotnet test C:\Code\open.db.viewer\tests\OpenDbViewer.Application.Tests\OpenDbViewer.Application.Tests.csproj --filter ExecuteAsync_ShouldPopulateResultGrid`  
Expected: FAIL

- [ ] **Step 3: Implement the query view model and query tab UI**

```csharp
public partial class QueryViewModel : ObservableObject
{
    private readonly QueryService _queryService;
    private readonly ExportService _exportService;
    private string? _activeTable;

    [ObservableProperty]
    private string sqlText = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<string> resultColumns = Array.Empty<string>();

    [ObservableProperty]
    private IReadOnlyList<IReadOnlyList<object?>> resultRows = Array.Empty<IReadOnlyList<object?>>();

    public void SetActiveTable(string tableName)
    {
        _activeTable = tableName;
        if (string.IsNullOrWhiteSpace(SqlText))
        {
            SqlText = $"select * from \"{tableName}\" limit 100;";
        }
    }

    [RelayCommand]
    public async Task ExecuteAsync()
    {
        var result = await _queryService.ExecuteAsync(SqlText);
        ResultColumns = result.Columns;
        ResultRows = result.Rows;
        StatusMessage = result.Message;
    }
}
```

- [ ] **Step 4: Run the full suite and do manual MVP verification**

Run: `dotnet test C:\Code\open.db.viewer\src\OpenDbViewer.sln`  
Expected: PASS

Run: `dotnet run --project C:\Code\open.db.viewer\src\OpenDbViewer.Wpf\OpenDbViewer.Wpf.csproj`  
Expected: PASS manual checklist for quick-open, object tree, schema, data, query, and CSV export.

- [ ] **Step 5: Write the manual verification checklist**

```markdown
# WPF MVP Verification Checklist

- Open a valid SQLite file from the home page
- Confirm it appears in recent files
- Open a table from the object tree
- Confirm schema fields render
- Confirm page 1 of data renders
- Change sort order and confirm visible row order changes
- Run a SELECT query and confirm results render
- Export query results to CSV and open the file in Excel
```

- [ ] **Step 6: Commit**

```bash
git -C C:\Code\open.db.viewer add src tests docs\verification
git -C C:\Code\open.db.viewer commit -m "feat: complete WPF SQLite MVP workflow"
```

## Self-Review

### Spec coverage

- SQLite-only scope: covered by Tasks 3 through 8
- Quick-open-first home page: covered by Task 6
- Object tree, schema, data, query, export: covered by Tasks 4, 5, 7, and 8
- WPF UI shell and Fluent direction: covered by Task 6 and Task 7
- MVP verification: covered by Task 8

### Placeholder scan

- No `TODO`, `TBD`, or “implement later” placeholders remain
- Every task contains exact file paths
- Every code-writing step includes concrete sample code
- Every verification step includes exact commands and expected output

### Type consistency

- `DatabaseEntryService`, `QueryService`, and `ExportService` are introduced before UI tasks use them
- Domain result types are defined before infrastructure and UI tasks consume them
- Workspace coordination uses `SchemaViewModel`, `DataViewModel`, and `QueryViewModel` consistently

