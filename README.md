# SQLite Viewer

基于 .NET 10 + WPF 的 **安全优先 SQLite 桌面查看器**（Windows）。定位为日常排查与分析：只读打开、对象浏览、分页数据、SQL 查询与导出；不做网格单元格编辑，也不做多数据库客户端。

技术标识：仓库目录 `sqlite.viewer`，程序集根命名空间 `Sqlite.Viewer.*`，发布/AppData 名 `SqliteViewer`。

## 分层结构

| 项目 | 职责 |
|------|------|
| `Sqlite.Viewer.Domain` | 领域模型、展示格式化、标识符工具 |
| `Sqlite.Viewer.Application` | 应用服务、抽象接口、SQL 分类与语句拆分 |
| `Sqlite.Viewer.Infrastructure.Sqlite` | SQLite 访问、流式 CSV、本地 JSON 存储 |
| `Sqlite.Viewer.Shell` | WPF 界面、MVVM、主题与 DI 组合根 |
| `tests/*` | 按层划分的单元测试与烟雾测试 |

## 功能概览

- 打开 `*.db` / `*.sqlite` / `*.sqlite3`，最近/固定列表
- 对象浏览器：表 / 视图 / 索引 / 触发器 / 系统表开关，搜索与右键（复制名称·DDL、在查询中打开、刷新）
- 结构页：列信息、DDL、索引、触发器；索引/触发器对象可单独查看
- 数据页：分页、跳页、总行数、列排序、列筛选（包含/等于/空/非空）、NULL·BLOB 展示、复制、导出当前页 / **流式导出整表**
- 查询页：执行全部 / 当前语句、EXPLAIN、历史与收藏、结果上限与取消/超时、可写模式（高风险二次确认）
- 主题：浅色 / 深色 / 跟随系统（持久化）
- 设置：默认页大小、查询行数上限、超时

设置与历史存储于：`%LocalAppData%\SqliteViewer\`

## 技术栈

- .NET 10 / WPF (`net10.0-windows`)
- CommunityToolkit.Mvvm、WPF-UI、Microsoft.Data.Sqlite、Microsoft.Extensions.DependencyInjection、SharpVectors.Wpf

## 快速开始

```powershell
dotnet restore Sqlite.Viewer.slnx
dotnet build Sqlite.Viewer.slnx -c Debug
dotnet run --project .\src\Sqlite.Viewer.Shell\Sqlite.Viewer.Shell.csproj
```

运行全部测试：

```powershell
dotnet test Sqlite.Viewer.slnx -c Debug
```

## 发布打包

### 自包含单目录（推荐分发 zip）

```powershell
dotnet publish .\src\Sqlite.Viewer.Shell\Sqlite.Viewer.Shell.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=false `
  -o .\artifacts\SqliteViewer-win-x64
```

将 `artifacts\SqliteViewer-win-x64` 目录压缩为 zip 即可分发。入口为 `SqliteViewer.exe`。

### 框架依赖（需目标机已装 .NET 10 桌面运行时）

```powershell
dotnet publish .\src\Sqlite.Viewer.Shell\Sqlite.Viewer.Shell.csproj `
  -c Release `
  -r win-x64 `
  --self-contained false `
  -o .\artifacts\SqliteViewer-fd
```

### 版本

当前程序集版本见 `Sqlite.Viewer.Shell.csproj` 的 `<Version>`（关于页会显示）。

## 可写模式说明

1. 默认 **只读连接**；写入/DDL 会被拦截。
2. 点击「启用可写」需确认。
3. 高风险语句（DDL、VACUUM/ANALYZE/ATTACH 等维护、写 PRAGMA）默认 **二次确认**。
4. 可勾选「本会话不再确认高风险」跳过后续高风险确认（切回只读会清除该标记）。
5. DML（INSERT/UPDATE/DELETE）在可写模式下直接执行，不再额外确认。

## 已知限制

- 仅 SQLite，无 PostgreSQL / MySQL / SQL Server
- 不提供网格内单元格编辑 / 行编辑器
- 查询页「导出已加载结果」不重跑全量 SQL（受结果行数上限约束）
- SQL 编辑器暂无语法高亮 / 智能补全（可选增强）
- 查询「执行当前语句」在未同步光标索引时默认取最后一条语句

## 目录结构

```text
sqlite.viewer
├─ Sqlite.Viewer.slnx
├─ .github/workflows/ci.yml
├─ src
│  ├─ Sqlite.Viewer.Domain
│  ├─ Sqlite.Viewer.Application
│  ├─ Sqlite.Viewer.Infrastructure.Sqlite
│  └─ Sqlite.Viewer.Shell
│       └─ Views/Workspace/   # ObjectExplorer / Schema / Data / Query 面板
└─ tests
   ├─ Sqlite.Viewer.Domain.Tests
   ├─ Sqlite.Viewer.Application.Tests
   ├─ Sqlite.Viewer.Infrastructure.Sqlite.Tests
   └─ Sqlite.Viewer.Shell.Tests
```

## 架构要点

- **整洁架构**：依赖向内（Shell → Infrastructure → Application → Domain）
- **MVVM**：业务在 ViewModel，View code-behind 仅处理选择/排序等 UI 事件
- **只读优先**：可写显式 opt-in + 危险 SQL 策略
- **DI 组合根**：`ServiceCollectionExtensions.AddSqliteViewerWpfServices()`

### 工作台视图拆分

| 控件 | 职责 |
|------|------|
| `WorkspaceHostPage` | 布局壳、标签页框架、空状态 |
| `ObjectExplorerPanel` | 左侧对象列表 |
| `SchemaPanel` | 结构标签内容 |
| `DataPanel` | 数据浏览与导出 |
| `QueryPanel` | SQL 查询 |
| `WorkspaceStyles.xaml` | 工作台共享样式 |

## CI

GitHub Actions（`windows-latest`）在 push/PR 到 `main`/`master` 时执行：

```text
dotnet restore / build / test Sqlite.Viewer.slnx -c Release
```

## 开发约定

- 领域模型 → `Domain`；流程 → `Application`；SQLite/文件 → `Infrastructure.Sqlite`；界面状态 → `Shell`
- 不引入未经批准的 fallback 逻辑
- 提交前展示 diff，经确认后再 commit
