# Open.db.viewer

Open.db.viewer 是一个基于 .NET 10 和 WPF 的 SQLite 桌面查看工具，面向 Windows 使用场景，提供数据库打开、最近/固定记录管理、表结构浏览、分页查看数据、执行 SQL 以及 CSV 导出等能力。

项目当前采用清晰的分层结构：

- `Open.Db.Viewer.Domain`：领域模型与结果对象
- `Open.Db.Viewer.Application`：应用服务与抽象接口
- `Open.Db.Viewer.Infrastructure.Sqlite`：SQLite 访问、元数据读取、CSV 导出、本地存储
- `Open.Db.Viewer.Shell`：WPF 桌面界面、主题切换、页面与 ViewModel
- `tests/*`：按分层划分的单元测试与界面烟雾测试

## 功能概览

- 打开 SQLite 数据库文件，支持 `*.db`、`*.sqlite`、`*.sqlite3`
- 维护最近使用和已固定数据库列表
- 首页提供快速打开入口与最近/固定摘要
- 工作台左侧对象浏览器支持表搜索与切换
- 结构页展示列信息、行数、页大小、SQLite 版本、建表 SQL、索引、触发器
- 数据页支持分页浏览、切换每页行数、按列排序、刷新当前页、导出当前页 CSV
- 查询页支持执行自定义 SQL，并提供常用模板：
  - 查询前 100 行
  - 统计行数
  - 查看表结构
- 查询结果支持导出为 CSV
- 支持浅色、深色、跟随系统三种主题模式

## 技术栈

- .NET 10
- WPF (`net10.0-windows`)
- [CommunityToolkit.Mvvm](https://www.nuget.org/packages/CommunityToolkit.Mvvm)
- [WPF UI](https://www.nuget.org/packages/WPF-UI)
- [Microsoft.Data.Sqlite](https://www.nuget.org/packages/Microsoft.Data.Sqlite)
- `Microsoft.Extensions.DependencyInjection`
- `SharpVectors.Wpf`

## 目录结构

```text
open.db.viewer
├─ src
│  ├─ Open.Db.Viewer.Domain
│  ├─ Open.Db.Viewer.Application
│  ├─ Open.Db.Viewer.Infrastructure.Sqlite
│  ├─ Open.Db.Viewer.Shell
│  └─ Open.Db.Viewer.slnx
└─ tests
   ├─ Open.Db.Viewer.Domain.Tests
   ├─ Open.Db.Viewer.Application.Tests
   ├─ Open.Db.Viewer.Infrastructure.Sqlite.Tests
   └─ Open.Db.Viewer.Shell.Tests
```

## 软件架构

### 架构概览

项目遵循**整洁架构（Clean Architecture）**，从内向外依次为：Domain（领域模型）→ Application（应用服务与抽象）→ Infrastructure.Sqlite（数据访问实现）→ Shell（WPF 组合根）。

```
┌─────────────────────────────────────────────┐
│  Shell (WPF UI / MVVM / DI 组合根)          │
│  ┌───────────────────────────────────────┐  │
│  │  Infrastructure.Sqlite               │  │
│  │  ┌─────────────────────────────────┐  │  │
│  │  │  Application (接口 + 服务)     │  │  │
│  │  │  ┌───────────────────────────┐  │  │  │
│  │  │  │  Domain (record 模型)    │  │  │  │
│  │  │  └───────────────────────────┘  │  │  │
│  │  └─────────────────────────────────┘  │  │
│  └───────────────────────────────────────┘  │
└─────────────────────────────────────────────┘
```

依赖方向始终向内：外层依赖内层，内层不感知外层。Domain 无任何外部依赖，Application 只依赖 Domain，Infrastructure 依赖 Application（实现其接口），Shell 作为组合根引用所有项目并完成 DI 注册。

### 启动流程

```
App.OnStartup()
  ├─ 创建 ServiceCollection
  ├─ 调用 AddOpenDbViewerWpfServices() 注册全部单例
  │    ├─ 抽象 → 实现绑定（IDatabaseEntryRepository → FileDatabaseEntryRepository 等）
  │    ├─ Application 服务（DatabaseEntryService, QueryService, ExportService）
  │    ├─ Infrastructure 服务（SqliteDatabaseInspector, SqliteTableDataReader 等）
  │    ├─ Shell 服务（ThemeService）
  │    └─ 全部 ViewModel + MainWindow
  ├─ BuildServiceProvider()
  ├─ ThemeService.Initialize() → 应用系统/浅色/深色主题
  ├─ 从容器解析 MainWindow，DataContext = ShellViewModel
  └─ 显示主窗口，ShellViewModel 默认展示 HomeLandingViewModel
```

### 架构模式

**MVVM（Model-View-ViewModel）**

基于 CommunityToolkit.Mvvm 的源生生成器实现，所有 ViewModel 继承 `ObservableObject`，使用 `[ObservableProperty]` 声明可绑定属性，`[RelayCommand]` 声明命令。View（XAML）通过 `DataContext` 绑定 ViewModel，无直接耦合。

**依赖注入（DI）**

使用 `Microsoft.Extensions.DependencyInjection`。所有服务、ViewModel 和 MainWindow 均注册为 **Singleton**（桌面单窗口应用的全生命周期单例是合适的）。注册集中在 `ServiceCollectionExtensions.AddOpenDbViewerWpfServices()`。

**模板驱动的视图解析**

Shell 使用 WPF `DataTemplateSelector`（`PageTemplateSelector`）将 ViewModel 类型映射到对应的 UserControl。当 `ShellViewModel.CurrentContentViewModel` 切换时，WPF 的 `ContentControl` 自动选择正确的视图模板。无需 `INavigationService` 或 ViewLocator 抽象。

```
ShellViewModel.CurrentContentViewModel 变化
  → ContentControl 触发模板选择
  → PageTemplateSelector 根据 ViewModel 类型返回 DataTemplate
  → 对应 UserControl 渲染，DataContext 自动继承
```

### 导航流

```
首页 (HomeLandingPage)
  │ "打开数据库" → FileDialogService.PickSqliteFile()
  │            → DatabaseEntryService.OpenAsync()
  │            → HomeLanding.DatabaseOpenedAsync 回调
  │            → ShellViewModel 切换到 Workspace + 加载数据库
  ▼
工作台 (WorkspaceHostPage)
  ├─ 左侧: ObjectExplorerPage（表列表 + 搜索）
  │        │ 点击表 → WorkspaceHostPage 事件处理
  │        │       → DatabaseWorkspaceViewModel.SelectNodeAsync()
  │        ▼
  ├─ 标签页区域（结构 / 数据 / 查询）
  │   ├─ SchemaPage: SqliteDatabaseInspector → 列信息/索引/触发器/DDL
  │   ├─ DataPage:   SqliteTableDataReader → 分页数据 + 排序 + CSV 导出
  │   └─ QueryPage:  SqliteQueryExecutor → 自定义 SQL + 结果导出
  │
  └─ RequestReturnHome 回调 → ShellViewModel.ReturnHomeAsync()
      返回首页
```

### ViewModel 组合关系

```
ShellViewModel (顶层 Shell，持有导航状态)
  ├─ HomeLandingViewModel (首页)
  ├─ DatabaseWorkspaceViewModel (工作台协调者)
  │    ├─ ObjectExplorerViewModel (对象浏览器)
  │    ├─ SchemaViewModel (结构)
  │    ├─ DataViewModel (分页数据)
  │    └─ QueryViewModel (SQL 查询)
  ├─ SettingsViewModel (设置)
  └─ AboutViewModel (关于)
```

跨 ViewModel 通信通过直接委托回调完成（非事件聚合器）：
- `HomeLanding.DatabaseOpenedAsync` → `Workspace.OpenWorkspaceAsync`（首页打开数据库后导航到工作台）
- `Workspace.RequestReturnHomeAsync` → `ShellViewModel.ReturnHomeAsync`（工作台返回首页）

### 主题系统

```
ThemeService
  ├─ ThemePreference: System / Light / Dark
  ├─ Initialize(): 应用当前偏好
  ├─ ToggleTheme(): 在 Light/Dark 间切换
  ├─ 监听 Windows SystemEvents.UserPreferenceChanged（跟随系统时自动同步）
  ├─ ApplicationThemeManager.Apply(): 交由 WPF-UI 切换 Light/Dark 资源
  └─ ThemeChanged 事件 → MainWindow 更新 Logo 与切换按钮图标
```

### 关键设计决策

| 决策 | 说明 |
|---|---|
| 全部 Singleton | 桌面单窗口应用无需 Scoped/Transient 生命周期，全单例简化了对象图 |
| 仅 SQLite | 整个检查/读取/查询管线硬编码 SQLite（`sqlite_master`、`PRAGMA`），非通用数据库客户端 |
| 只读操作 | 连接使用 `ReadWriteCreate` 模式，但应用不提供写/DDL 功能 |
| 无线导航服务 | 导航通过 `CurrentContentViewModel` 切换 + `DataTemplateSelector` 完成，无需抽象层 |
| 无事件聚合器 | ViewModel 间通信使用 `ShellViewModel` 构造函数直接连接委托回调 |
| 文件持久化 | 最近/固定数据库记录存储为本地 JSON（`%LocalAppData%\OpenDbViewer\database-entries.json`） |

### 依赖关系图

```
Shell (WPF, MVVM, 组合根)
  ├── Infrastructure.Sqlite (数据访问, CSV, 本地存储)
  │     └── Application (接口 + 服务)
  │           └── Domain (record 模型, 零依赖)
  ├── WPF-UI 4.3.0 (FluentWindow, NavigationView, 主题基础设施)
  ├── CommunityToolkit.Mvvm 8.4.2 (源生 MVVM)
  ├── Microsoft.Extensions.DependencyInjection
  ├── Microsoft.Data.Sqlite 10.0.7
  └── SharpVectors.Wpf 1.8.5 (SVG 图标)
```

## 核心模块说明

### 1. 数据库入口与记录管理

`DatabaseEntryService` 负责数据库打开流程，并将最近使用/固定记录交给 `IDatabaseEntryRepository` 持久化。当前默认实现为 `FileDatabaseEntryRepository`，数据会写入：

```text
%LocalAppData%\OpenDbViewer\database-entries.json
```

### 2. SQLite 元数据与数据读取

`Open.Db.Viewer.Infrastructure.Sqlite` 提供了几类核心能力：

- `SqliteDatabaseInspector`：读取表列表、表结构、行数、索引、触发器、建表 SQL
- `SqliteTableDataReader`：按页读取表数据，并支持排序
- `SqliteQueryExecutor`：执行任意 SQL，返回列信息、结果集、耗时和反馈消息
- `CsvExportWriter`：将结果集导出为 CSV

### 3. 桌面工作台

`DatabaseWorkspaceViewModel` 是工作台的核心协调者，组合了：

- `ObjectExplorerViewModel`：对象浏览器与表过滤
- `SchemaViewModel`：结构与元数据展示
- `DataViewModel`：分页数据浏览
- `QueryViewModel`：SQL 编辑、执行与结果导出

这部分基本对应主界面的三个工作区域：

1. 左侧对象浏览器
2. 中右侧结构/数据/查询标签页
3. 顶部数据库概览与刷新操作

## 运行环境

- Windows 10/11
- .NET 10 SDK

由于 UI 基于 WPF，当前项目不面向 macOS / Linux 直接运行。

## 快速开始

### 还原依赖

```powershell
dotnet restore Open.Db.Viewer.slnx
```

### 构建

```powershell
dotnet build Open.Db.Viewer.slnx
```

### 运行桌面程序

open.db.viewer

```powershell
dotnet run --project .\src\Open.Db.Viewer.Shell\Open.Db.Viewer.Shell.csproj
```

## 使用流程

1. 在首页点击“打开数据库”
2. 选择 SQLite 文件
3. 在左侧对象浏览器中搜索并选择数据表
4. 在“结构”页查看字段、DDL、索引、触发器等信息
5. 在“数据”页分页浏览表数据，必要时导出当前页
6. 在“查询”页执行 SQL，或使用内置模板快速生成常用语句

## 测试

运行全部测试：

```powershell
dotnet test Open.Db.Viewer.slnx
```

当前测试项目覆盖：

- 领域模型行为
- 应用服务逻辑
- SQLite 查询/结构读取/本地存储
- Shell 层 ViewModel 与主窗口烟雾测试

## 当前能力边界

根据现有代码，项目当前重点是“SQLite 查看与分析”，而不是通用数据库客户端。已确认的边界包括：

- 目前仅接入 SQLite
- 对象浏览器当前聚焦数据表，不包含视图、索引树或触发器树导航
- 主题切换已实现，但主题偏好当前是运行期状态，不是持久化设置

## 后续可扩展方向

- 增加视图、索引、触发器等对象级导航
- 引入查询历史与 SQL 收藏
- 支持结果筛选、复制单元格、导出全部分页数据
- 补充应用打包与发布说明
- 增加截图、演示 GIF 或安装包下载信息

## 开发说明

仓库使用依赖注入组织 Shell 层对象，入口注册集中在：

- `src/Open.Db.Viewer.Shell/ServiceCollectionExtensions.cs`

如果要继续扩展功能，建议遵循当前分层：

- 领域模型放在 `Domain`
- 业务流程放在 `Application`
- 数据访问与文件存储放在 `Infrastructure.Sqlite`
- 界面与交互状态放在 `Shell`

这样可以保持测试边界清晰，也更方便后续替换数据源或增加新 UI。
